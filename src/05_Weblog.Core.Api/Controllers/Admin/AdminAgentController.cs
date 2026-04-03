using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;
using Weblog.Core.Service.Implements;
using Weblog.Core.Service.Interfaces;
using SqlSugar;

namespace Weblog.Core.Api.Controllers.Admin;

/// <summary>
/// 后台智能 Agent —— 通过自然语言对话完成博客管理操作
/// 支持：文章增删改查、分类/标签管理、仪表盘查询、评论管理
/// </summary>
[Route("api/admin/agent")]
[ApiController]
[Authorize]
[RequireRole("admin")]
public class AdminAgentController : ControllerBase
{
    private readonly IArticleService  _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService      _tagService;
    private readonly IDashboardService _dashboardService;
    private readonly ICommentService  _commentService;
    private readonly AiProviderSelector _providerSelector;
    private readonly ProviderRegistry _registry;
    private readonly ILogger<AdminAgentController> _logger;
    private readonly ISqlSugarClient _db;

    public AdminAgentController(
        IArticleService articleService,
        ICategoryService categoryService,
        ITagService tagService,
        IDashboardService dashboardService,
        ICommentService commentService,
        AiProviderSelector providerSelector,
        ProviderRegistry registry,
        ILogger<AdminAgentController> logger,
        ISqlSugarClient db)
    {
        _articleService   = articleService;
        _categoryService = categoryService;
        _tagService       = tagService;
        _dashboardService = dashboardService;
        _commentService  = commentService;
        _providerSelector = providerSelector;
        _registry         = registry;
        _logger           = logger;
        _db               = db;
    }

    [HttpGet("models")]
    public Result<List<AgentModelInfo>> GetAvailableModels()
    {
        var models = new List<AgentModelInfo>();

        try
        {
            // 获取已启用的 Provider 名称列表（小写）
            var enabledProviders = _db.Queryable<AiProvider>().Where(p => p.IsEnabled).ToList();
            var enabledProviderNames = enabledProviders.Select(p => p.Name.ToLower()).ToHashSet();

            // 优先从 Provider 注册表获取所有已启用 Provider 的模型列表
            var allMetadata = _registry.GetAllMetadata();
            foreach (var meta in allMetadata)
            {
                if (!enabledProviderNames.Contains(meta.Name.ToLower())) continue;

                foreach (var modelId in meta.Models)
                {
                    models.Add(new AgentModelInfo
                    {
                        Id = modelId,
                        Name = modelId,
                        Provider = meta.DisplayName ?? meta.Name
                    });
                }
            }

            // 合并 AiModel 表中已配置的模型（用户自定义的模型配置优先）
            var configuredModels = _db.Queryable<AiModel>()
                .Where(m => m.IsEnabled)
                .OrderByDescending(m => m.IsDefault)
                .OrderBy(m => m.CreateTime, OrderByType.Desc)
                .ToList();

            foreach (var m in configuredModels)
            {
                if (!enabledProviderNames.Contains(m.Type?.ToLower() ?? "")) continue;

                // 替换或新增（以 AiModel 配置优先，允许用户覆盖名称）
                var existing = models.FirstOrDefault(x =>
                    x.Id.Equals(m.Model, StringComparison.OrdinalIgnoreCase) &&
                    x.Provider.Equals(m.Type, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // 更新显示名称（保留注册表的 Id 和 Provider）
                    existing.Name = string.IsNullOrEmpty(m.Name) ? m.Model : m.Name;
                }
                else
                {
                    // AiModel 中有但注册表没有的模型（如自定义部署的模型），也加入列表
                    models.Add(new AgentModelInfo
                    {
                        Id = m.Model,
                        Name = string.IsNullOrEmpty(m.Name) ? m.Model : m.Name,
                        Provider = m.Type
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load available models");
        }

        return Result<List<AgentModelInfo>>.Ok(models);
    }

    [HttpPost("chat")]
    public async Task Chat([FromBody] AgentChatRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection   = "keep-alive";

        // 1. 选 Provider
        var (provider, apiKey, error) = await _providerSelector.SelectAsync(type: AiProviderType.Chat);
        if (provider == null || apiKey == null)
        {
            await SendEvent(new { type = "error", message = error ?? "没有可用的 AI Provider，请先在 AI 设置中配置" });
            return;
        }

        var settings = request.Settings ?? new AgentSettingsDto();
        var maxTurns = Math.Clamp(settings.MaxTurns, 1, 10);
        var temperature = settings.Temperature > 0 ? settings.Temperature : 0.2;
        var maxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : 4096;
        var enabledTools = settings.EnabledTools;

        // 2. 构建消息列表
        var systemPrompt = await BuildSystemPromptAsync(settings.SystemPrompt);
        var messages = new List<AiChatMessage> { new() { Role = "system", Content = systemPrompt } };
        
        // 加入历史消息
        if (request.History?.Count > 0)
        {
            foreach (var h in request.History.TakeLast(10))
            {
                messages.Add(new AiChatMessage { Role = h.Role, Content = h.Content ?? "" });
            }
        }

        // 加入当前用户消息
        messages.Add(new AiChatMessage { Role = "user", Content = request.Message });

        var tools = BuildTools(enabledTools);
        var model = request.Model ?? provider.DefaultModel ?? "";

        // 若带 sessionId，从数据库补充历史消息
        if (!string.IsNullOrEmpty(request.SessionId) && (request.History == null || request.History.Count == 0))
        {
            try
            {
                var saved = await _db.Queryable<AiConversation>()
                    .FirstAsync(c => c.SessionId == request.SessionId);
                if (saved?.Messages != null)
                {
                    var hist = System.Text.Json.JsonSerializer
                        .Deserialize<List<AgentHistoryItem>>(saved.Messages);
                    if (hist?.Count > 0)
                        messages.AddRange(hist.TakeLast(20)
                            .Select(h => new AiChatMessage { Role = h.Role, Content = h.Content ?? "" }));
                }
            }
            catch { }
        }

        _logger.LogInformation("Agent chat started. Model: {Model}, MaxTurns: {MaxTurns}, Tools: {ToolCount}", 
            model, maxTurns, tools.Count);

        try
        {
            for (int turn = 0; turn < maxTurns; turn++)
            {
                _logger.LogInformation("Turn {Turn}: Sending request with {MsgCount} messages", turn + 1, messages.Count);

                var aiRequest = new AiChatRequest
                {
                    Model       = model,
                    Messages    = messages,
                    Temperature = temperature,
                    MaxTokens   = maxTokens,
                    Tools       = tools.Count > 0 ? tools : null,
                    ToolChoice  = "auto"
                };

                await SendEvent(new { type = "turn", turn = turn + 1, maxTurns });

                var response = await provider.ChatAsync(aiRequest, apiKey, ct);

                _logger.LogInformation("Turn {Turn}: Response - Content length: {Len}, ToolName: {Tool}, FinishReason: {FR}", 
                    turn + 1, response.Content?.Length ?? 0, response.ToolName ?? "null", response.FinishReason ?? "null");

                // 如果 AI 调用了工具
                if (!string.IsNullOrEmpty(response.ToolName))
                {
                    await SendEvent(new { type = "thinking", tool = response.ToolName, message = $"正在执行：{TranslateToolName(response.ToolName)}" });
                    await SendEvent(new { type = "log", level = "info", message = $"[Turn {turn + 1}] 调用工具: {response.ToolName}, 参数: {response.Content}" });

                    var toolResult = await ExecuteToolAsync(response, request);

                    await SendEvent(new { type = "log", level = "info", message = $"[Turn {turn + 1}] 工具执行完成: {response.ToolName}, 结果长度: {toolResult.Length}" });
                    await SendEvent(new { type = "tool_result", tool = response.ToolName, args = response.Content, result = toolResult });

                    // 将 assistant 的 tool_calls 消息加入历史
                    messages.Add(new AiChatMessage
                    {
                        Role    = "assistant",
                        Content = response.Content ?? "",
                        ToolCalls = new List<AiToolCall>
                        {
                            new()
                            {
                                Id   = response.ToolCallId ?? $"call_{turn}",
                                Type = "function",
                                Function = new AiToolCallFunction
                                {
                                    Name      = response.ToolName ?? "",
                                    Arguments = response.Content ?? "{}"
                                }
                            }
                        }
                    });
                    
                    // 将 tool 结果加入历史
                    messages.Add(new AiChatMessage
                    {
                        Role       = "tool",
                        ToolCallId = response.ToolCallId ?? $"call_{turn}",
                        Content    = toolResult
                    });

                    _logger.LogInformation("Turn {Turn}: Tool {Tool} executed. Result length: {Len}", 
                        turn + 1, response.ToolName, toolResult.Length);
                    continue;
                }

                // 没有工具调用，流式输出结果
                _logger.LogInformation("Turn {Turn}: Final response, streaming", turn + 1);
                
                var streamRequest = new AiChatRequest
                {
                    Model       = model,
                    Messages    = messages,
                    Temperature = temperature,
                    MaxTokens   = maxTokens
                };
                
                await foreach (var chunk in provider.ChatStreamAsync(streamRequest, apiKey, ct))
                {
                    if (!string.IsNullOrEmpty(chunk))
                        await SendEvent(new { type = "chunk", content = chunk });
                }
                break;
            }

            await SendEvent(new { type = "done" });

            // 保存 Agent 会话历史
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                try
                {
                    var histItems = messages
                        .Where(m => m.Role is "user" or "assistant")
                        .Select(m => new AgentHistoryItem { Role = m.Role, Content = m.Content })
                        .ToList();
                    var histJson = System.Text.Json.JsonSerializer.Serialize(histItems);
                    var existing = await _db.Queryable<AiConversation>()
                        .FirstAsync(c => c.SessionId == request.SessionId);
                    if (existing == null)
                        await _db.Insertable(new AiConversation
                        {
                            SessionId    = request.SessionId,
                            UserId       = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value ?? "admin",
                            PluginId     = "agent",
                            Messages     = histJson,
                            Model        = model,
                            ProviderName = provider.Name,
                            CreatedAt    = DateTime.Now,
                            UpdatedAt    = DateTime.Now
                        }).ExecuteCommandAsync();
                    else
                    {
                        existing.Messages  = histJson;
                        existing.UpdatedAt = DateTime.Now;
                        await _db.Updateable(existing).ExecuteCommandAsync();
                    }
                }
                catch (Exception saveEx) { _logger.LogWarning(saveEx, "Failed to save agent session"); }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AdminAgent: stream cancelled");
            await SendEvent(new { type = "done" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminAgent: error");
            await SendEvent(new { type = "error", message = ex.Message });
        }
    }

    private async Task<string> ExecuteToolAsync(AiChatResponse aiResponse, AgentChatRequest req)
    {
        var toolName = aiResponse.ToolName ?? "";
        var argsJson = aiResponse.Content ?? "{}";

        Dictionary<string, JsonElement> args = new();
        try
        {
            args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson) ?? new();
        }
        catch { }

        try
        {
            var result = toolName switch
            {
                "get_dashboard"          => await Tool_GetDashboard(),
                "get_articles"           => await Tool_GetArticles(args),
                "search_articles"        => await Tool_SearchArticles(args),
                "get_article"            => await Tool_GetArticle(args),
                "get_article_content"    => await Tool_GetArticleContent(args),
                "delete_article"         => await Tool_DeleteArticle(args),
                "toggle_article_top"     => await Tool_ToggleArticleTop(args),
                "create_article"         => await Tool_CreateArticle(args),
                "update_article"         => await Tool_UpdateArticle(args),
                "batch_delete_articles"  => await Tool_BatchDeleteArticles(args),
                "get_categories"         => await Tool_GetCategories(),
                "create_category"        => await Tool_CreateCategory(args),
                "delete_category"        => await Tool_DeleteCategory(args),
                "get_tags"               => await Tool_GetTags(),
                "create_tag"             => await Tool_CreateTag(args),
                "delete_tag"             => await Tool_DeleteTag(args),
                "get_comments"           => await Tool_GetComments(args),
                "delete_comment"         => await Tool_DeleteComment(args),
                "approve_comment"        => await Tool_ApproveComment(args),
                "reject_comment"         => await Tool_RejectComment(args),
                _ => $"未知工具：{toolName}"
            };

            // 记录操作日志（非查询类操作）
            if (!toolName.StartsWith("get_") && toolName != "search_articles")
            {
                var target = toolName.Contains("article") ? "article"
                    : toolName.Contains("category") ? "category"
                    : toolName.Contains("tag") ? "tag"
                    : toolName.Contains("comment") ? "comment"
                    : "unknown";
                var action = toolName.Replace($"_{target}", "").Replace("_", "");
                var targetId = TryGetLong(args, "id");
                await LogAgentOperation(action, target, targetId, result, req.Message, argsJson);
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"工具执行失败：{ex.Message}";
        }
    }

    private async Task<string> Tool_GetDashboard()
    {
        var data = await _dashboardService.GetDashboardAsync();
        return JsonSerializer.Serialize(new
        {
            articles   = data.articleTotalCount,
            categories = data.categoryTotalCount,
            tags       = data.tagTotalCount,
            totalPv    = data.pvTotalCount
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_GetArticles(Dictionary<string, JsonElement> args)
    {
        var keyword  = TryGetString(args, "keyword");
        var page     = TryGetInt(args, "page", 1);
        var pageSize = TryGetInt(args, "pageSize", 10);

        int total;
        IEnumerable<object>? list;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // 关键词过滤：直接用 DB 查询 Title
            var query = _db.Queryable<Article>()
                .Where(a => !a.IsDeleted && a.Title.Contains(keyword));
            total = await query.CountAsync();
            var articles = await query
                .OrderByDescending(a => a.Weight)
                .OrderBy(a => a.CreateTime, OrderByType.Desc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            list = articles.Select(a => new
            {
                id         = a.Id,
                title      = a.Title,
                isTop      = a.Weight > 0,
                status     = a.Status,
                createTime = a.CreateTime
            });
        }
        else
        {
            var result = await _articleService.GetAdminPageAsync(new PageRequest
            {
                PageNum  = page,
                PageSize = pageSize
            });
            total = result.Total;
            list = result.List?.Select(a => new
            {
                id         = a.Id,
                title      = a.Title,
                isTop      = a.IsTop,
                status     = a.Status,
                createTime = a.CreateTime
            });
        }

        return JsonSerializer.Serialize(new { total, list }, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_GetArticle(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        var article = await _articleService.GetByIdAsync(id);
        if (article == null) return $"文章 {id} 不存在";
        return JsonSerializer.Serialize(new
        {
            article.Id,
            article.Title,
            article.IsTop,
            article.Status,
            article.CreateTime
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_SearchArticles(Dictionary<string, JsonElement> args)
    {
        var keyword = TryGetString(args, "keyword");
        if (string.IsNullOrWhiteSpace(keyword)) return "参数错误：keyword 不能为空";

        var articles = await _db.Queryable<Article>()
            .Where(a => !a.IsDeleted && a.Title.Contains(keyword))
            .OrderByDescending(a => a.Weight)
            .OrderBy(a => a.CreateTime, OrderByType.Desc)
            .Take(20)
            .ToListAsync();

        if (articles.Count == 0)
            return $"没有找到标题含「{keyword}」的文章";

        var list = articles.Select(a => new
        {
            id         = a.Id,
            title      = a.Title,
            status     = a.Status == 1 ? "已发布" : "未发布",
            isTop      = a.Weight > 0,
            createTime = a.CreateTime
        });
        return JsonSerializer.Serialize(new { count = articles.Count, keyword, list },
            new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_GetArticleContent(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        var article = await _articleService.GetByIdAsync(id);
        if (article == null) return $"文章 {id} 不存在";

        return JsonSerializer.Serialize(new
        {
            article.Id,
            article.Title,
            article.Summary,
            article.Content,
            article.CategoryId,
            article.CategoryName,
            article.Status,
            article.IsTop,
            tags = article.Tags?.Select(t => new { t.Id, t.Name }),
            article.CreateTime,
            article.UpdateTime
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_CreateArticle(Dictionary<string, JsonElement> args)
    {
        var title      = TryGetString(args, "title");
        var content    = TryGetString(args, "content");
        var summary    = TryGetString(args, "summary");
        var categoryId = TryGetLong(args, "categoryId");
        var status     = TryGetInt(args, "status", 1);

        if (string.IsNullOrWhiteSpace(title)) return "参数错误：title 不能为空";
        if (categoryId <= 0) return "参数错误：categoryId 不能为空，请先用 get_categories 查询分类列表";

        // 解析标签 ID 数组
        List<long>? tagIds = null;
        if (args.TryGetValue("tagIds", out var tagIdsEl) && tagIdsEl.ValueKind == JsonValueKind.Array)
            tagIds = tagIdsEl.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Number)
                .Select(e => e.GetInt64()).ToList();

        var req = new CreateArticleRequest
        {
            Title      = title,
            Content    = content ?? "",
            Summary    = summary,
            Cover      = "",
            CategoryId = categoryId,
            TagIds     = tagIds,
            Status     = status
        };

        var result = await _articleService.CreateAsync(req);
        return $"文章「{result.Title}」已创建（ID: {result.Id}，状态: {(result.Status == 1 ? "已发布" : "草稿")}）";
    }

    private async Task<string> Tool_UpdateArticle(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        // 先加载现有数据，再用入参覆盖（避免丢失字段）
        var existing = await _articleService.GetByIdAsync(id);
        if (existing == null) return $"文章 {id} 不存在";

        var req = new UpdateArticleRequest
        {
            Id         = id,
            Title      = TryGetString(args, "title")      ?? existing.Title,
            Content    = TryGetString(args, "content")    ?? existing.Content ?? "",
            Summary    = TryGetString(args, "summary")    ?? existing.Summary,
            Cover      = existing.Cover,
            CategoryId = TryGetLong(args, "categoryId") is long cid && cid > 0 ? cid : existing.CategoryId,
            Status     = args.ContainsKey("status")     ? TryGetInt(args, "status", existing.Status) : existing.Status,
            Weight     = args.ContainsKey("weight")     ? TryGetInt(args, "weight", existing.Weight) : existing.Weight,
            TagIds     = existing.TagIds
        };

        // 如果传了 tagIds 就覆盖
        if (args.TryGetValue("tagIds", out var tagIdsEl) && tagIdsEl.ValueKind == JsonValueKind.Array)
            req.TagIds = tagIdsEl.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Number)
                .Select(e => e.GetInt64()).ToList();

        var result = await _articleService.UpdateAsync(req);
        return $"文章「{result.Title}」（ID: {result.Id}）已更新";
    }

    private async Task<string> Tool_BatchDeleteArticles(Dictionary<string, JsonElement> args)
    {
        if (!args.TryGetValue("ids", out var idsEl) || idsEl.ValueKind != JsonValueKind.Array)
            return "参数错误：ids 不能为空，需传入文章 ID 数组";

        var ids = idsEl.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Number)
            .Select(e => e.GetInt64())
            .Distinct()
            .Take(20)
            .ToList();

        if (ids.Count == 0) return "参数错误：ids 数组为空";

        var succeeded = new List<long>();
        var failed    = new List<long>();

        foreach (var id in ids)
        {
            var ok = await _articleService.DeleteAsync(id);
            if (ok) succeeded.Add(id);
            else    failed.Add(id);
        }

        var msg = $"批量删除完成：成功 {succeeded.Count} 篇，失败 {failed.Count} 篇";
        if (succeeded.Count > 0) msg += $"\n成功 ID: {string.Join(", ", succeeded)}";
        if (failed.Count > 0)    msg += $"\n失败 ID: {string.Join(", ", failed)}";
        return msg;
    }

    private async Task<string> Tool_DeleteArticle(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        var ok = await _articleService.DeleteAsync(id);
        return ok ? $"文章 {id} 已成功删除" : $"文章 {id} 删除失败";
    }

    private async Task<string> Tool_ToggleArticleTop(Dictionary<string, JsonElement> args)
    {
        var id    = TryGetLong(args, "id");
        var isTop = TryGetBool(args, "isTop");
        if (id <= 0) return "参数错误：id 不能为空";

        var ok = await _articleService.UpdateIsTopAsync(id, isTop);
        return ok ? $"文章 {id} 已{(isTop ? "置顶" : "取消置顶")}" : "操作失败";
    }

    private async Task<string> Tool_GetCategories()
    {
        var list = await _categoryService.GetListAsync();
        return JsonSerializer.Serialize(list?.Select(c => new { c.Id, c.Name, articleCount = c.ArticlesTotal }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_CreateCategory(Dictionary<string, JsonElement> args)
    {
        var name = TryGetString(args, "name");
        if (string.IsNullOrWhiteSpace(name)) return "参数错误：name 不能为空";

        var result = await _categoryService.CreateAsync(new CreateCategoryRequest { Name = name });
        return $"分类「{result.Name}」已创建（ID: {result.Id}）";
    }

    private async Task<string> Tool_DeleteCategory(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        var hasArticle = await _categoryService.HasArticleAsync(id);
        if (hasArticle) return "该分类下还有文章，无法删除";

        var ok = await _categoryService.DeleteAsync(id);
        return ok ? $"分类 {id} 已删除" : "删除失败";
    }

    private async Task<string> Tool_GetTags()
    {
        var list = await _tagService.GetListAsync();
        return JsonSerializer.Serialize(list?.Select(t => new { t.Id, t.Name, articleCount = t.ArticlesTotal }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_CreateTag(Dictionary<string, JsonElement> args)
    {
        var name = TryGetString(args, "name");
        if (string.IsNullOrWhiteSpace(name)) return "参数错误：name 不能为空";

        var result = await _tagService.CreateAsync(new CreateTagRequest { Name = name });
        return $"标签「{result.Name}」已创建（ID: {result.Id}）";
    }

    private async Task<string> Tool_DeleteTag(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        var hasArticle = await _tagService.HasArticleAsync(id);
        if (hasArticle) return "该标签下还有文章，无法删除";

        var ok = await _tagService.DeleteAsync(id);
        return ok ? $"标签 {id} 已删除" : "删除失败";
    }

    private async Task<string> Tool_GetComments(Dictionary<string, JsonElement> args)
    {
        var keyword  = TryGetString(args, "keyword");
        var page     = TryGetInt(args, "page", 1);
        var pageSize = TryGetInt(args, "pageSize", 10);

        var result = await _commentService.GetAdminPageAsync(new CommentPageRequest
        {
            PageNum  = page,
            PageSize = pageSize
        });

        var list = result.List?.Select(c => new
        {
            id        = c.Id,
            content   = c.Content.Length > 50 ? c.Content[..50] + "..." : c.Content,
            nickname  = c.Nickname,
            status    = c.Status == 1 ? "已通过" : c.Status == 0 ? "待审核" : "已拒绝",
            isSecret  = c.IsSecret,
            createTime= c.CreateTime
        });

        return JsonSerializer.Serialize(new { total = result.Total, list }, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> Tool_DeleteComment(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        var ok = await _commentService.DeleteAsync(id);
        return ok ? $"评论 {id} 已删除" : "删除失败";
    }

    private async Task<string> Tool_ApproveComment(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";

        var ok = await _commentService.ApproveAsync(id);
        return ok ? $"评论 {id} 已通过审核" : "操作失败";
    }

    private async Task<string> Tool_RejectComment(Dictionary<string, JsonElement> args)
    {
        var id = TryGetLong(args, "id");
        if (id <= 0) return "参数错误：id 不能为空";
        var reason = TryGetString(args, "reason") ?? "内容不合规";

        var ok = await _commentService.RejectAsync(id, reason);
        return ok ? $"评论 {id} 已拒绝：{reason}" : "操作失败";
    }

    private static List<object> BuildTools(List<string>? enabledTools)
    {
        var allTools = new List<(string name, string desc, object? param)>
        {
            ("get_dashboard", "获取博客仪表盘统计信息：文章总数、分类数、标签数、总 PV", null),
            ("get_articles", "分页浏览文章列表，支持按关键词过滤标题", new { type = "object", properties = new { keyword = new { type = "string", description = "搜索关键词（可选）" }, page = new { type = "integer", description = "页码，默认1" }, pageSize = new { type = "integer", description = "每页条数，默认10" } }, required = Array.Empty<string>() }),
            ("search_articles", "按关键词搜索文章标题（最多返回20条，直接搜索无需翻页），优先使用此工具而不是 get_articles", new { type = "object", properties = new { keyword = new { type = "string", description = "搜索关键词" } }, required = new[] { "keyword" } }),
            ("get_article", "根据 ID 获取文章基本信息（不含正文）", new { type = "object", properties = new { id = new { type = "integer", description = "文章 ID" } }, required = new[] { "id" } }),
            ("get_article_content", "根据 ID 获取文章完整内容（包含正文、分类、标签等所有字段）", new { type = "object", properties = new { id = new { type = "integer", description = "文章 ID" } }, required = new[] { "id" } }),
            ("delete_article", "删除指定 ID 的文章", new { type = "object", properties = new { id = new { type = "integer", description = "文章 ID" } }, required = new[] { "id" } }),
            ("toggle_article_top", "设置或取消文章置顶", new { type = "object", properties = new { id = new { type = "integer", description = "文章 ID" }, isTop = new { type = "boolean", description = "true=置顶，false=取消" } }, required = new[] { "id", "isTop" } }),
            ("create_article", "创建新文章（注意：需要先用 get_categories 获取有效的 categoryId）", new { type = "object", properties = new { title = new { type = "string", description = "文章标题" }, content = new { type = "string", description = "文章正文（Markdown 格式）" }, summary = new { type = "string", description = "摘要（可选）" }, categoryId = new { type = "integer", description = "分类 ID（必填，先用 get_categories 查询）" }, tagIds = new { type = "array", items = new { type = "integer" }, description = "标签 ID 列表（可选）" }, status = new { type = "integer", description = "状态：1=发布，0=草稿，默认1" } }, required = new[] { "title", "categoryId" } }),
            ("update_article", "更新已有文章的字段（只传需要修改的字段，未传字段保留原值）", new { type = "object", properties = new { id = new { type = "integer", description = "文章 ID" }, title = new { type = "string", description = "新标题（可选）" }, content = new { type = "string", description = "新正文（可选）" }, summary = new { type = "string", description = "新摘要（可选）" }, categoryId = new { type = "integer", description = "新分类 ID（可选）" }, status = new { type = "integer", description = "新状态：1=发布，0=草稿（可选）" }, weight = new { type = "integer", description = "置顶权重：0=不置顶，>0=置顶（可选）" }, tagIds = new { type = "array", items = new { type = "integer" }, description = "新标签 ID 列表（可选）" } }, required = new[] { "id" } }),
            ("batch_delete_articles", "批量删除多篇文章（最多20篇）", new { type = "object", properties = new { ids = new { type = "array", items = new { type = "integer" }, description = "文章 ID 数组" } }, required = new[] { "ids" } }),
            ("get_categories", "获取所有分类列表（包含 ID 和文章数量）", null),
            ("create_category", "创建新分类", new { type = "object", properties = new { name = new { type = "string", description = "分类名称" } }, required = new[] { "name" } }),
            ("delete_category", "删除指定分类", new { type = "object", properties = new { id = new { type = "integer", description = "分类 ID" } }, required = new[] { "id" } }),
            ("get_tags", "获取所有标签列表", null),
            ("create_tag", "创建新标签", new { type = "object", properties = new { name = new { type = "string", description = "标签名称" } }, required = new[] { "name" } }),
            ("delete_tag", "删除指定标签", new { type = "object", properties = new { id = new { type = "integer", description = "标签 ID" } }, required = new[] { "id" } }),
            ("get_comments", "分页查询评论列表", new { type = "object", properties = new { keyword = new { type = "string", description = "搜索关键词" }, page = new { type = "integer", description = "页码，默认1" }, pageSize = new { type = "integer", description = "每页条数，默认10" } }, required = Array.Empty<string>() }),
            ("delete_comment", "删除指定 ID 的评论", new { type = "object", properties = new { id = new { type = "integer", description = "评论 ID" } }, required = new[] { "id" } }),
            ("approve_comment", "通过审核指定评论", new { type = "object", properties = new { id = new { type = "integer", description = "评论 ID" } }, required = new[] { "id" } }),
            ("reject_comment", "拒绝指定评论", new { type = "object", properties = new { id = new { type = "integer", description = "评论 ID" }, reason = new { type = "string", description = "拒绝原因" } }, required = new[] { "id" } }),
        };

        return allTools
            .Where(t => enabledTools == null || enabledTools.Count == 0 || enabledTools.Contains(t.name) || enabledTools.Contains("*"))
            .Select(t => (object)new
            {
                type = "function",
                function = new
                {
                    name = t.name,
                    description = t.desc,
                    parameters = t.param ?? new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            }).ToList();
    }

    private async Task<string> BuildSystemPromptAsync(string? customPrompt)
    {
        // 从数据库加载配置文件内容
        var configs = await _db.Queryable<AiAgentConfig>().ToListAsync();
        var identity = configs.FirstOrDefault(c => c.FileName == "IDENTITY")?.Content ?? DefaultConfigs["IDENTITY"];
        var soul = configs.FirstOrDefault(c => c.FileName == "SOUL")?.Content ?? DefaultConfigs["SOUL"];
        var tools = configs.FirstOrDefault(c => c.FileName == "TOOLS")?.Content ?? DefaultConfigs["TOOLS"];

        var basePrompt = $@"你是一个精确的博客管理助手。你必须使用工具来获取真实数据，禁止编造任何数据。

{identity}

{soul}

## 核心规则：
1. **必须使用工具**：任何关于文章、分类、标签、评论、统计数据的问题，你必须调用相应工具获取真实数据
2. **禁止编造**：永远不要自己生成文章列表、统计数据等，必须通过工具查询
3. **危险操作需要确认**：对于删除、创建、置顶、审核等操作，你必须：
   - 先明确告诉用户将要执行什么操作
   - 征求用户同意：「⚠️ 操作确认：您确定要 [操作内容] 吗？此操作不可撤销。请回复「确认」继续。」
   - 只有用户明确回复「确认」后才执行危险操作
4. **查询操作直接执行**：查看、统计等操作可以直接执行，不需要确认

## 工具调度策略：
- **搜索文章**：优先用 `search_articles`（关键词搜索），不需要翻页的场景总是用它
- **浏览文章**：用 `get_articles`（分页列表），适合「查看第N页」「显示最新N篇」等场景
- **读文章全文**：用 `get_article_content`（包含正文），不是 `get_article`（只有基本信息）
- **创建/更新文章**：必须先用 `get_categories` 获取分类列表，确认 categoryId 存在后再操作
- **批量删除**：用 `batch_delete_articles`，一次传入所有 ID 数组

{tools}

## 回答格式：
- 直接展示工具返回的真实数据
- 不要添加任何虚假信息
- 用表格或列表展示结构化数据
- 操作完成后说明执行结果
- 危险操作前必须先发送确认提示";

        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            return customPrompt + "\n\n" + basePrompt;
        }

        return basePrompt;
    }

    private async Task SendEvent(object data)
    {
        var json = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    private static string TranslateToolName(string? name) => name switch
    {
        "get_dashboard"          => "查询仪表盘数据",
        "get_articles"           => "查询文章列表",
        "search_articles"        => "搜索文章",
        "get_article"            => "获取文章基本信息",
        "get_article_content"    => "读取文章全文",
        "delete_article"         => "删除文章",
        "toggle_article_top"     => "设置文章置顶",
        "create_article"         => "创建文章",
        "update_article"         => "更新文章",
        "batch_delete_articles"  => "批量删除文章",
        "get_categories"         => "获取分类列表",
        "create_category"        => "创建分类",
        "delete_category"        => "删除分类",
        "get_tags"               => "获取标签列表",
        "create_tag"             => "创建标签",
        "delete_tag"             => "删除标签",
        "get_comments"           => "获取评论列表",
        "delete_comment"         => "删除评论",
        "approve_comment"        => "审核评论",
        "reject_comment"         => "拒绝评论",
        _                            => name ?? "执行操作"
    };

    private static string? TryGetString(Dictionary<string, JsonElement> args, string key)
        => args.TryGetValue(key, out var v) ? v.GetString() : null;

    private static int TryGetInt(Dictionary<string, JsonElement> args, string key, int def = 0)
    {
        if (args.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetInt32();
        return def;
    }

    private static long TryGetLong(Dictionary<string, JsonElement> args, string key)
    {
        if (args.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetInt64();
        return 0;
    }

    private static bool TryGetBool(Dictionary<string, JsonElement> args, string key, bool def = false)
    {
        if (args.TryGetValue(key, out var v))
            return v.ValueKind == JsonValueKind.True;
        return def;
    }

    // ──────── 配置文件管理 API ────────

    private static readonly Dictionary<string, string> DefaultConfigs = new()
    {
        { "AGENTS", "# AI Agent 配置\n\n定义 AI Agent 的行为模式和能力范围。\n\n## 角色\n- 博客管理助手\n- 内容分析助手\n\n## 能力\n- 文章管理（增删改查）\n- 分类和标签管理\n- 评论审核\n- 数据统计分析" },
        { "IDENTITY", "# AI Agent 身份\n\n## 名称\n博客智能助手\n\n## 描述\n我是一个专业的博客管理助手，帮助管理员高效管理博客内容。\n\n## 个性\n- 专业、严谨\n- 友善、耐心\n- 注重数据准确性\n- 操作前主动确认" },
        { "SOUL", "# AI Agent 灵魂\n\n## 核心价值观\n- 数据真实性：绝不编造数据\n- 操作安全性：危险操作必须确认\n- 用户友好性：清晰展示信息\n\n## 行为准则\n1. 始终使用工具获取真实数据\n2. 删除操作前必须二次确认\n3. 以表格或列表展示结构化数据\n4. 操作完成后汇报结果" },
        { "USER", "# 用户配置\n\n## 偏好\n- 语言：中文\n- 数据展示：表格优先\n- 确认方式：对话框确认\n\n## 权限\n- 文章管理：完全控制\n- 分类管理：完全控制\n- 标签管理：完全控制\n- 评论管理：完全控制" },
        { "MEMORY", "# 记忆配置\n\n## 会话记忆\n- 保留最近 10 条对话历史\n- 跨会话不保留记忆\n\n## 上下文\n- 当前博客统计信息\n- 最近操作记录" },
        { "TOOLS", "# 工具配置\n\n## 可用工具\n\n### 查询类（直接执行）\n- `get_dashboard` - 仪表盘统计\n- `get_articles` - 文章分页列表（支持关键词过滤）\n- `search_articles` - **关键词搜索文章**（最多20条，推荐使用）\n- `get_article` - 文章基本信息\n- `get_article_content` - 文章完整内容（含正文）\n- `get_categories` - 分类列表\n- `get_tags` - 标签列表\n- `get_comments` - 评论列表\n\n### 操作类（需要确认）\n- `create_article` - 创建文章（需先查分类ID）\n- `update_article` - 更新文章（只传修改的字段）\n- `delete_article` - 删除单篇文章\n- `batch_delete_articles` - 批量删除文章\n- `toggle_article_top` - 文章置顶\n- `create_category` - 创建分类\n- `delete_category` - 删除分类\n- `create_tag` - 创建标签\n- `delete_tag` - 删除标签\n- `delete_comment` - 删除评论\n- `approve_comment` - 审核通过评论\n- `reject_comment` - 拒绝评论" }
    };

    /// <summary>
    /// 获取所有配置文件列表
    /// </summary>
    [HttpGet("configs")]
    public async Task<Result<List<AgentConfigDto>>> GetConfigs()
    {
        var configs = await _db.Queryable<AiAgentConfig>().ToListAsync();
        var result = new List<AgentConfigDto>();

        foreach (var kvp in DefaultConfigs)
        {
            var config = configs.FirstOrDefault(c => c.FileName == kvp.Key);
            if (config == null)
            {
                // 自动初始化默认配置到数据库
                var newConfig = new AiAgentConfig
                {
                    FileName = kvp.Key,
                    Content = kvp.Value,
                    Description = GetConfigDescription(kvp.Key),
                    UpdatedAt = DateTime.Now
                };
                await _db.Insertable(newConfig).ExecuteCommandAsync();
                config = newConfig;
            }
            result.Add(new AgentConfigDto
            {
                FileName = kvp.Key,
                Content = config.Content,
                Description = config.Description ?? GetConfigDescription(kvp.Key),
                UpdatedAt = config.UpdatedAt
            });
        }

        return Result<List<AgentConfigDto>>.Ok(result);
    }

    /// <summary>
    /// 获取单个配置文件
    /// </summary>
    [HttpGet("configs/{fileName}")]
    public async Task<Result<AgentConfigDto>> GetConfig(string fileName)
    {
        fileName = fileName.ToUpper();
        if (!DefaultConfigs.ContainsKey(fileName))
            return Result<AgentConfigDto>.Fail($"未知的配置文件: {fileName}");

        var config = await _db.Queryable<AiAgentConfig>()
            .FirstAsync(c => c.FileName == fileName);

        return Result<AgentConfigDto>.Ok(new AgentConfigDto
        {
            FileName = fileName,
            Content = config?.Content ?? DefaultConfigs[fileName],
            Description = config?.Description ?? GetConfigDescription(fileName),
            UpdatedAt = config?.UpdatedAt ?? DateTime.Now
        });
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    [HttpPut("configs/{fileName}")]
    public async Task<Result<string>> SaveConfig(string fileName, [FromBody] SaveAgentConfigRequest request)
    {
        fileName = fileName.ToUpper();
        if (!DefaultConfigs.ContainsKey(fileName))
            return Result<string>.Fail($"未知的配置文件: {fileName}");

        var existing = await _db.Queryable<AiAgentConfig>()
            .FirstAsync(c => c.FileName == fileName);

        if (existing != null)
        {
            existing.Content = request.Content;
            existing.UpdatedAt = DateTime.Now;
            await _db.Updateable(existing).ExecuteCommandAsync();
        }
        else
        {
            var config = new AiAgentConfig
            {
                FileName = fileName,
                Content = request.Content,
                Description = GetConfigDescription(fileName),
                UpdatedAt = DateTime.Now
            };
            await _db.Insertable(config).ExecuteCommandAsync();
        }

        // 记录操作日志
        await LogAgentOperation("update", "config", 0, $"更新配置文件: {fileName}");

        return Result<string>.Ok("保存成功");
    }

    /// <summary>
    /// 重置配置文件为默认值
    /// </summary>
    [HttpPost("configs/{fileName}/reset")]
    public async Task<Result<AgentConfigDto>> ResetConfig(string fileName)
    {
        fileName = fileName.ToUpper();
        if (!DefaultConfigs.ContainsKey(fileName))
            return Result<AgentConfigDto>.Fail($"未知的配置文件: {fileName}");

        var existing = await _db.Queryable<AiAgentConfig>()
            .FirstAsync(c => c.FileName == fileName);

        if (existing != null)
        {
            await _db.Deleteable(existing).ExecuteCommandAsync();
        }

        await LogAgentOperation("reset", "config", 0, $"重置配置文件: {fileName}");

        return Result<AgentConfigDto>.Ok(new AgentConfigDto
        {
            FileName = fileName,
            Content = DefaultConfigs[fileName],
            Description = GetConfigDescription(fileName),
            UpdatedAt = DateTime.Now
        });
    }

    private static string GetConfigDescription(string fileName) => fileName switch
    {
        "AGENTS" => "AI Agent 行为模式和能力范围配置",
        "IDENTITY" => "AI Agent 身份和个性设定",
        "SOUL" => "AI Agent 核心价值观和行为准则",
        "USER" => "用户偏好和权限配置",
        "MEMORY" => "会话记忆和上下文配置",
        "TOOLS" => "可用工具列表和使用规则",
        _ => ""
    };

    // ──────── 操作日志 API ────────

    /// <summary>
    /// 获取操作日志列表
    /// </summary>
    [HttpGet("logs")]
    public async Task<Result<AgentLogPageResult>> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? action = null, [FromQuery] string? target = null)
    {
        var query = _db.Queryable<AiAgentLog>();

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrEmpty(target))
            query = query.Where(l => l.Target == target);

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<AgentLogPageResult>.Ok(new AgentLogPageResult
        {
            Total = total,
            List = list.Select(l => new AgentLogDto
            {
                Id = l.Id,
                Action = l.Action,
                Target = l.Target,
                TargetId = l.TargetId,
                Description = l.Description,
                UserMessage = l.UserMessage,
                AiResponse = l.AiResponse,
                Status = l.Status,
                Operator = l.Operator,
                CreatedAt = l.CreatedAt
            }).ToList()
        });
    }

    /// <summary>
    /// 清空操作日志
    /// </summary>
    [HttpDelete("logs")]
    public async Task<Result<string>> ClearLogs()
    {
        await _db.Deleteable<AiAgentLog>().ExecuteCommandAsync();
        return Result<string>.Ok("日志已清空");
    }

    // ──────── Agent 会话历史 ────────

    [HttpGet("sessions")]
    public async Task<Result<List<AgentSessionDto>>> GetAgentSessions()
    {
        var sessions = await _db.Queryable<AiConversation>()
            .Where(c => c.PluginId == "agent")
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
        var dtos = sessions.Select(s => new AgentSessionDto
        {
            SessionId = s.SessionId,
            Title     = GetAgentSessionTitle(s.Messages),
            Model     = s.Model,
            CreatedAt = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = s.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
        return Result<List<AgentSessionDto>>.Ok(dtos);
    }

    [HttpGet("session/{sessionId}")]
    public async Task<Result<AgentSessionDetailDto>> GetAgentSession(string sessionId)
    {
        var session = await _db.Queryable<AiConversation>()
            .FirstAsync(c => c.SessionId == sessionId && c.PluginId == "agent");
        if (session == null) return Result<AgentSessionDetailDto>.Fail("会话不存在");
        List<AgentHistoryItem> history = new();
        try { history = System.Text.Json.JsonSerializer.Deserialize<List<AgentHistoryItem>>(session.Messages ?? "[]") ?? new(); } catch { }
        return Result<AgentSessionDetailDto>.Ok(new AgentSessionDetailDto
        {
            SessionId = session.SessionId, Model = session.Model, History = history,
            CreatedAt = session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = session.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    [HttpDelete("session/{sessionId}")]
    public async Task<Result<bool>> DeleteAgentSession(string sessionId)
    {
        await _db.Deleteable<AiConversation>()
            .Where(c => c.SessionId == sessionId && c.PluginId == "agent")
            .ExecuteCommandAsync();
        return Result<bool>.Ok(true);
    }

    private static string GetAgentSessionTitle(string? messagesJson)
    {
        try
        {
            if (string.IsNullOrEmpty(messagesJson)) return "新会话";
            var msgs = System.Text.Json.JsonSerializer.Deserialize<List<AgentHistoryItem>>(messagesJson);
            var first = msgs?.FirstOrDefault(m => m.Role == "user");
            if (first != null)
                return first.Content?.Length > 40 ? first.Content[..40] + "..." : first.Content ?? "新会话";
        }
        catch { }
        return "新会话";
    }

    /// <summary>
    /// 记录操作日志（内部方法）
    /// </summary>
    private async Task LogAgentOperation(string action, string target, long targetId, string description, string? userMessage = null, string? aiResponse = null)
    {
        try
        {
            var log = new AiAgentLog
            {
                Action = action,
                Target = target,
                TargetId = targetId,
                Description = description,
                UserMessage = userMessage?.Length > 2000 ? userMessage[..2000] : userMessage,
                AiResponse = aiResponse?.Length > 2000 ? aiResponse[..2000] : aiResponse,
                Status = "success",
                Operator = "admin",
                CreatedAt = DateTime.Now
            };
            await _db.Insertable(log).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write agent operation log");
        }
    }
}

public class AgentSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class AgentSessionDetailDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public List<AgentHistoryItem> History { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class AgentChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<AgentHistoryItem>? History { get; set; }
    public string? Model { get; set; }
    public AgentSettingsDto? Settings { get; set; }
    /// <summary>会话 ID，传入后自动加载/保存历史记录</summary>
    public string? SessionId { get; set; }
}

public class AgentSettingsDto
{
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 4096;
    public int MaxTurns { get; set; } = 5;
    public string? SystemPrompt { get; set; }
    public List<string>? EnabledTools { get; set; }
}

public class AgentHistoryItem
{
    public string Role { get; set; } = "user";
    public string? Content { get; set; }
}

public class AgentModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class AgentConfigDto
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveAgentConfigRequest
{
    public string Content { get; set; } = string.Empty;
}

public class AgentLogDto
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public long TargetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? UserMessage { get; set; }
    public string? AiResponse { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AgentLogPageResult
{
    public int Total { get; set; }
    public List<AgentLogDto> List { get; set; } = new();
}
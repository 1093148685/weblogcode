using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Plugins;
using Weblog.Core.Service.AI.Providers;
using SqlSugar;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/ai")]
[ApiController]
public class AiChatController : ControllerBase
{
    private readonly IAiKernel _aiKernel;
    private readonly PluginManager _pluginManager;
    private readonly ILogger<AiChatController> _logger;
    private readonly ISqlSugarClient _db;

    public AiChatController(IAiKernel aiKernel, PluginManager pluginManager, ILogger<AiChatController> logger, ISqlSugarClient db)
    {
        _aiKernel = aiKernel;
        _pluginManager = pluginManager;
        _logger = logger;
        _db = db;
    }

    [HttpGet("models")]
    public Result<List<AiModelInfo>> GetAvailableModels()
    {
        var models = new List<AiModelInfo>();
        
        try
        {
            var enabledProviders = _db.Queryable<AiProvider>().Where(p => p.IsEnabled).ToList();
            
            foreach (var provider in enabledProviders)
            {
                var providerModels = GetModelsForProvider(provider.Name);
                models.AddRange(providerModels);
            }
            
            if (models.Count == 0)
            {
                models = GetDefaultModels();
            }
        }
        catch
        {
            models = GetDefaultModels();
        }

        return Result<List<AiModelInfo>>.Ok(models);
    }

    [HttpGet("models-simple")]
    public Result<List<AiModelInfo>> GetSimpleModels()
    {
        return Result<List<AiModelInfo>>.Ok(GetDefaultModels());
    }

    private List<AiModelInfo> GetModelsForProvider(string providerName)
    {
        var providerLower = providerName.ToLower();

        return providerLower switch
        {
            var p when p.Contains("deepseek") => new List<AiModelInfo>
            {
                new() { Id = "deepseek-chat", Name = "DeepSeek V3", Provider = "deepseek" },
                new() { Id = "deepseek-coder", Name = "DeepSeek Coder", Provider = "deepseek" },
                new() { Id = "deepseek-reasoner", Name = "DeepSeek R1", Provider = "deepseek" }
            },
            var p when p.Contains("openai") || p.Contains("gpt") => new List<AiModelInfo>
            {
                new() { Id = "gpt-4o-mini", Name = "GPT-4o Mini", Provider = "openai" },
                new() { Id = "gpt-4o", Name = "GPT-4o", Provider = "openai" },
                new() { Id = "gpt-4-turbo", Name = "GPT-4 Turbo", Provider = "openai" },
                new() { Id = "gpt-3.5-turbo", Name = "GPT-3.5 Turbo", Provider = "openai" }
            },
            var p when p.Contains("claude") || p.Contains("anthropic") => new List<AiModelInfo>
            {
                new() { Id = "claude-sonnet-4-20250514", Name = "Claude Sonnet 4", Provider = "claude" },
                new() { Id = "claude-3-5-sonnet-20241022", Name = "Claude 3.5 Sonnet", Provider = "claude" },
                new() { Id = "claude-3-5-haiku-20241022", Name = "Claude 3.5 Haiku", Provider = "claude" },
                new() { Id = "claude-3-opus-20240229", Name = "Claude 3 Opus", Provider = "claude" }
            },
            var p when p.Contains("gemini") || p.Contains("google") => new List<AiModelInfo>
            {
                new() { Id = "gemini-2.0-flash", Name = "Gemini 2.0 Flash", Provider = "gemini" },
                new() { Id = "gemini-1.5-pro", Name = "Gemini 1.5 Pro", Provider = "gemini" },
                new() { Id = "gemini-1.5-flash", Name = "Gemini 1.5 Flash", Provider = "gemini" }
            },
            var p when p.Contains("zhipu") || p.Contains("glm") || p.Contains("智谱") => new List<AiModelInfo>
            {
                new() { Id = "glm-4-plus", Name = "GLM-4 Plus", Provider = "zhipu" },
                new() { Id = "glm-4", Name = "GLM-4", Provider = "zhipu" },
                new() { Id = "glm-4-flash", Name = "GLM-4 Flash", Provider = "zhipu" },
                new() { Id = "glm-3-turbo", Name = "GLM-3 Turbo", Provider = "zhipu" }
            },
            var p when p.Contains("qianfan") || p.Contains("百度") || p.Contains("baidu") || p.Contains("ernie") => new List<AiModelInfo>
            {
                new() { Id = "ernie-4.0-8k", Name = "ERNIE 4.0", Provider = "qianfan" },
                new() { Id = "ernie-3.5-8k", Name = "ERNIE 3.5", Provider = "qianfan" },
                new() { Id = "ernie-speed-8k", Name = "ERNIE Speed", Provider = "qianfan" },
                new() { Id = "ernie-speed-128k", Name = "ERNIE Speed 128K", Provider = "qianfan" }
            },
            var p when p.Contains("minimax") => new List<AiModelInfo>
            {
                new() { Id = "MiniMax-M2.7", Name = "MiniMax M2.7", Provider = "minimax" },
                new() { Id = "MiniMax-M2.5", Name = "MiniMax M2.5", Provider = "minimax" },
                new() { Id = "MiniMax-M1", Name = "MiniMax M1", Provider = "minimax" }
            },
            var p when p.Contains("azure") => new List<AiModelInfo>
            {
                new() { Id = "gpt-4o", Name = "Azure GPT-4o", Provider = "azure" },
                new() { Id = "gpt-4o-mini", Name = "Azure GPT-4o Mini", Provider = "azure" }
            },
            _ => new List<AiModelInfo>()
        };
    }

    private List<AiModelInfo> GetDefaultModels()
    {
        return new List<AiModelInfo>
        {
            new() { Id = "deepseek-chat", Name = "DeepSeek V3", Provider = "deepseek" },
            new() { Id = "deepseek-reasoner", Name = "DeepSeek R1", Provider = "deepseek" },
            new() { Id = "gpt-4o-mini", Name = "GPT-4o Mini", Provider = "openai" },
            new() { Id = "gpt-4o", Name = "GPT-4o", Provider = "openai" },
            new() { Id = "claude-sonnet-4-20250514", Name = "Claude Sonnet 4", Provider = "claude" },
            new() { Id = "claude-3-5-sonnet-20241022", Name = "Claude 3.5 Sonnet", Provider = "claude" },
            new() { Id = "gemini-2.0-flash", Name = "Gemini 2.0 Flash", Provider = "gemini" },
            new() { Id = "glm-4-flash", Name = "GLM-4 Flash", Provider = "zhipu" },
            new() { Id = "MiniMax-M2.7", Name = "MiniMax M2.7", Provider = "minimax" }
        };
    }

    [HttpPost("chat")]
    [AllowAnonymous]
    public async Task Chat([FromBody] PortalChatRequest? request, CancellationToken ct)
    {
        if (request == null || request.Messages == null || request.Messages.Count == 0)
        {
            await SendError("消息不能为空");
            return;
        }

        var pluginEnabled = await CheckPluginEnabledAsync("chat_assistant");
        if (!pluginEnabled)
        {
            await SendError("AI 助手已禁用，请联系管理员开启");
            return;
        }

        var isAdmin = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value == "admin";
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                     ?? request.ClientId
                     ?? "anonymous";

        if (!isAdmin)
        {
            var usageResult = await CheckAndIncrementUsageAsync(userId);
            if (!usageResult.Success)
            {
                await SendError(usageResult.Message);
                return;
            }
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var fullResponse = new StringBuilder();
        var model = request.Model ?? "deepseek-chat";

        var pluginConfig = await LoadPluginConfigAsync("chat_assistant");
        var systemPrompt = pluginConfig.GetValueOrDefault("systemPrompt")?.ToString();
        var defaultSystemPrompt = systemPrompt;
        var temperature = GetFloatValue(pluginConfig, "temperature", 0.7f);
        var maxTokens = GetIntValue(pluginConfig, "maxTokens", 4096);

        try
        {
            var messages = request.Messages.Select(m => new AiChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList();

            if (!string.IsNullOrEmpty(request.ArticleContent))
            {
                var articlePrompt = $"【参考文章内容】\n标题：{request.ArticleTitle ?? "未知"}\n内容：{request.ArticleContent}\n\n";
                if (messages.First().Role == "system")
                {
                    messages[0].Content = messages[0].Content.Insert(0, articlePrompt);
                }
                else
                {
                    messages.Insert(0, new AiChatMessage { Role = "system", Content = articlePrompt });
                }
            }

            if (!string.IsNullOrEmpty(defaultSystemPrompt))
            {
                if (messages.First().Role == "system")
                {
                    messages[0].Content = messages[0].Content.Insert(0, defaultSystemPrompt + "\n\n");
                }
                else
                {
                    messages.Insert(0, new AiChatMessage { Role = "system", Content = defaultSystemPrompt });
                }
            }

            var aiRequest = new AiChatRequest
            {
                Model = model,
                Messages = messages,
                Temperature = temperature,
                MaxTokens = maxTokens
            };

            var selectResult = await _aiKernel.SelectProviderAsync(
                preferredProvider: GetProviderFromModel(model),
                type: AiProviderType.Chat
            );
            IAiProvider? provider = selectResult.provider;
            string? apiKey = selectResult.apiKey;
            string? error = selectResult.error;

            if (provider == null || string.IsNullOrEmpty(apiKey))
            {
                await SendError(error ?? "没有可用的 AI Provider");
                return;
            }

            await foreach (var chunk in provider.ChatStreamAsync(aiRequest, apiKey, ct))
            {
                if (string.IsNullOrEmpty(chunk))
                    continue;

                fullResponse.Append(chunk);
                await SendChunk(chunk);
            }

            await SendDone();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat stream cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat stream error");
            await SendError(ex.Message);
        }
    }

    private async Task<Dictionary<string, object>> LoadPluginConfigAsync(string pluginId)
    {
        try
        {
            var allPlugins = await _db.Queryable<AiPlugin>().ToListAsync();
            var dbPlugin = allPlugins.FirstOrDefault(p => p.PluginId == pluginId);
            if (dbPlugin?.Config != null && !string.IsNullOrEmpty(dbPlugin.Config))
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(dbPlugin.Config) ?? new Dictionary<string, object>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load plugin config for: {PluginId}", pluginId);
        }
        return new Dictionary<string, object>();
    }

    private async Task<bool> CheckPluginEnabledAsync(string pluginId)
    {
        try
        {
            var allPlugins = await _db.Queryable<AiPlugin>().ToListAsync();
            var dbPlugin = allPlugins.FirstOrDefault(p => p.PluginId == pluginId);
            if (dbPlugin == null)
            {
                return true;
            }
            return dbPlugin.IsEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check plugin enabled status for: {PluginId}", pluginId);
            return true;
        }
    }

    private async Task SendChunk(string content)
    {
        var data = JsonSerializer.Serialize(new { content });
        await Response.WriteAsync($"data: {data}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task SendError(string message)
    {
        var data = JsonSerializer.Serialize(new { error = message });
        await Response.WriteAsync($"data: {data}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task SendDone()
    {
        await Response.WriteAsync("data: [DONE]\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task<int> GetDailyLimitAsync()
    {
        try
        {
            var pluginConfig = await LoadPluginConfigAsync("chat_assistant");
            return GetIntValue(pluginConfig, "dailyLimit", 10);
        }
        catch
        {
            return 10;
        }
    }

    private async Task<(bool Success, string Message)> CheckAndIncrementUsageAsync(string userId)
    {
        var dailyLimit = await GetDailyLimitAsync();
        var today = DateTime.Now.Date;

        try
        {
            var todayLog = (await _db.Queryable<AiUsageLog>()
                .Where(l => l.UserId == userId && l.UsageDate == today)
                .ToListAsync())
                .FirstOrDefault();

            if (todayLog == null)
            {
                var newLog = new AiUsageLog
                {
                    UserId = userId,
                    UsageCount = 1,
                    UsageDate = today,
                    UpdatedAt = DateTime.Now
                };
                await _db.Insertable(newLog).ExecuteCommandAsync();
                return (true, $"今日剩余次数: {dailyLimit - 1}");
            }

            if (todayLog.UsageCount >= dailyLimit)
            {
                return (false, $"今日使用次数已用完（{dailyLimit}/{dailyLimit}），请明天再来或联系管理员");
            }

            todayLog.UsageCount++;
            todayLog.UpdatedAt = DateTime.Now;
            await _db.Updateable(todayLog).ExecuteCommandAsync();

            return (true, $"今日剩余次数: {dailyLimit - todayLog.UsageCount}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check usage for user: {UserId}", userId);
            return (true, "使用次数检查异常，已允许继续使用");
        }
    }

    [HttpGet("usage")]
    public async Task<Result<object>> GetUsageInfo([FromQuery] string? clientId = null)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                     ?? clientId
                     ?? "anonymous";
        var isAdmin = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value == "admin";

        if (isAdmin)
        {
            return Result<object>.Ok(new { IsAdmin = true, Remaining = -1, DailyLimit = -1, Message = "管理员不受限制" });
        }

        try
        {
            var dailyLimit = await GetDailyLimitAsync();
            var today = DateTime.Now.Date;
            var todayLog = (await _db.Queryable<AiUsageLog>()
                .Where(l => l.UserId == userId && l.UsageDate == today)
                .ToListAsync())
                .FirstOrDefault();
            var currentCount = todayLog?.UsageCount ?? 0;

            return Result<object>.Ok(new
            {
                IsAdmin = false,
                Used = currentCount,
                Remaining = Math.Max(0, dailyLimit - currentCount),
                DailyLimit = dailyLimit,
                Message = currentCount >= dailyLimit ? "今日次数已用完" : $"今日已使用 {currentCount} 次，剩余 {dailyLimit - currentCount} 次"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get usage info for user: {UserId}", userId);
            return Result<object>.Ok(new { IsAdmin = false, Used = 0, Remaining = 10, DailyLimit = 10 });
        }
    }

    // ──────────────────── 会话管理 ────────────────────

    [HttpPost("session/save")]
    public async Task<Result<bool>> SaveSession([FromBody] SaveSessionRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.SessionId))
            return Result<bool>.Fail("SessionId 不能为空");

        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                     ?? request.ClientId
                     ?? "anonymous";
        try
        {
            var existing = (await _db.Queryable<AiConversation>()
                .Where(c => c.SessionId == request.SessionId)
                .ToListAsync())
                .FirstOrDefault();

            if (existing == null)
            {
                var conv = new AiConversation
                {
                    SessionId = request.SessionId,
                    UserId = userId,
                    PluginId = "chat_assistant",
                    Messages = request.Messages ?? "[]",
                    Model = request.Model ?? "",
                    ProviderName = request.Provider ?? "",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                await _db.Insertable(conv).ExecuteCommandAsync();
            }
            else
            {
                existing.Messages = request.Messages ?? existing.Messages;
                existing.Model = request.Model ?? existing.Model;
                existing.ProviderName = request.Provider ?? existing.ProviderName;
                existing.UpdatedAt = DateTime.Now;
                await _db.Updateable(existing).ExecuteCommandAsync();
            }

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session: {SessionId}", request.SessionId);
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpGet("sessions")]
    public async Task<Result<List<UserSessionDto>>> GetUserSessions([FromQuery] string? clientId = null)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                     ?? clientId
                     ?? "anonymous";
        try
        {
            var sessions = (await _db.Queryable<AiConversation>()
                .Where(c => c.UserId == userId && c.PluginId == "chat_assistant")
                .ToListAsync())
                .OrderByDescending(c => c.UpdatedAt)
                .Take(50)
                .Select(s => new UserSessionDto
                {
                    SessionId = s.SessionId,
                    Title = GetSessionTitle(s.Messages),
                    Messages = s.Messages,
                    Model = s.Model,
                    CreatedAt = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = s.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToList();

            return Result<List<UserSessionDto>>.Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions for user: {UserId}", userId);
            return Result<List<UserSessionDto>>.Fail(ex.Message);
        }
    }

    [HttpDelete("session/{sessionId}")]
    public async Task<Result<bool>> DeleteUserSession(string sessionId, [FromQuery] string? clientId = null)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                     ?? clientId
                     ?? "anonymous";
        try
        {
            await _db.Deleteable<AiConversation>()
                .Where(c => c.SessionId == sessionId && c.UserId == userId)
                .ExecuteCommandAsync();
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session: {SessionId}", sessionId);
            return Result<bool>.Fail(ex.Message);
        }
    }

    private static string GetSessionTitle(string messagesJson)
    {
        try
        {
            if (string.IsNullOrEmpty(messagesJson)) return "新会话";
            var messages = JsonSerializer.Deserialize<List<PortalChatMessageItem>>(messagesJson);
            var first = messages?.FirstOrDefault(m => m.Role == "user");
            if (first != null)
                return first.Content.Length > 30 ? first.Content[..30] + "..." : first.Content;
        }
        catch { }
        return "新会话";
    }

    private static string? GetProviderFromModel(string model)
    {
        var m = model.ToLower();

        if (m.StartsWith("deepseek")) return "deepseek";
        if (m.StartsWith("gpt-") || m.StartsWith("o1") || m.StartsWith("o3")) return "openai";
        if (m.StartsWith("claude")) return "claude";
        if (m.StartsWith("gemini")) return "gemini";
        if (m.StartsWith("glm")) return "zhipu";
        if (m.StartsWith("ernie") || m == "qianfan") return "qianfan";
        if (m.StartsWith("minimax")) return "minimax";

        return null;
    }

    private static float GetFloatValue(Dictionary<string, object> config, string key, float defaultValue)
    {
        if (config.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.GetSingle();
                }
                if (float.TryParse(value.ToString(), out var result))
                {
                    return result;
                }
            }
            catch { }
        }
        return defaultValue;
    }

    private static int GetIntValue(Dictionary<string, object> config, string key, int defaultValue)
    {
        if (config.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.GetInt32();
                }
                if (int.TryParse(value.ToString(), out var result))
                {
                    return result;
                }
            }
            catch { }
        }
        return defaultValue;
    }
}

public class PortalChatRequest
{
    public List<PortalChatMessageItem> Messages { get; set; } = new();
    public string? SessionId { get; set; }
    public string? ClientId { get; set; }
    public string? Model { get; set; }
    public string? ArticleContent { get; set; }
    public string? ArticleTitle { get; set; }
}

public class PortalChatMessageItem
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class AiModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class SaveSessionRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? Messages { get; set; }
    public string? Model { get; set; }
    public string? Provider { get; set; }
}

public class UserSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Messages { get; set; } = "[]";
    public string Model { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

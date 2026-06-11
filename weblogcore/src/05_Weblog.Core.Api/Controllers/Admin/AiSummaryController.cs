using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.Interfaces;
using SqlSugar;
using Weblog.Core.Repository;
using Weblog.Core.Api.Services;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/ai-summary")]
[ApiController]
[Authorize]
public class AiSummaryController : ControllerBase
{
    private readonly IAiSummaryService _aiSummaryService;
    private readonly IAiModelService _aiModelService;
    private readonly DbContext _dbContext;
    private readonly InMemoryCacheService _cache;

    public AiSummaryController(IAiSummaryService aiSummaryService, IAiModelService aiModelService, DbContext dbContext, InMemoryCacheService cache)
    {
        _aiSummaryService = aiSummaryService;
        _aiModelService = aiModelService;
        _dbContext = dbContext;
        _cache = cache;
    }

    [HttpGet]
    public async Task<Result<List<AiModelDto>>> GetAllModels()
    {
        var result = await _aiModelService.GetAllAsync();
        return Result<List<AiModelDto>>.Ok(result);
    }

    [HttpGet("model/{id}")]
    public async Task<Result<AiModelDto>> GetModel(long id)
    {
        var result = await _aiModelService.GetByIdAsync(id);
        return Result<AiModelDto>.Ok(result ?? new AiModelDto());
    }

    [HttpPost]
    [RequireRole("admin")]
    public async Task<Result<AiSummaryDto>> Create([FromBody] CreateAiSummaryRequest request)
    {
        var result = await _aiSummaryService.CreateAsync(request);
        return Result<AiSummaryDto>.Ok(result);
    }

    [HttpPut]
    [RequireRole("admin")]
    public async Task<Result<AiSummaryDto>> Update([FromBody] UpdateAiSummaryRequest request)
    {
        var result = await _aiSummaryService.UpdateAsync(request);
        return Result<AiSummaryDto>.Ok(result);
    }

    [HttpDelete("{id}")]
    [RequireRole("admin")]
    public async Task<Result<bool>> DeleteModel(long id)
    {
        await _aiModelService.DeleteAsync(id);
        return Result<bool>.Ok(true);
    }

    [HttpGet("article/{articleId}")]
    public async Task<Result<AiSummaryDto>> GetByArticleId(long articleId)
    {
        var result = await _aiSummaryService.GetByArticleIdAsync(articleId);
        return Result<AiSummaryDto>.Ok(result ?? new AiSummaryDto());
    }

    [HttpPost("test/{id}")]
    [RequireRole("admin")]
    public async Task<Result<string>> Test(long id)
    {
        try
        {
            var model = await _aiModelService.GetByIdAsync(id);
            if (model == null || string.IsNullOrEmpty(model.ApiKey))
            {
                return Result<string>.Fail("模型不存在或API Key为空");
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            string testUrl = "";
            var headers = new Dictionary<string, string>();

            switch (model.Type.ToLower())
            {
                case "openai":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/models";
                    headers["Authorization"] = $"Bearer {model.ApiKey}";
                    break;
                case "claude":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/messages";
                    headers["x-api-key"] = model.ApiKey;
                    headers["anthropic-version"] = "2023-06-01";
                    break;
                case "azure":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/models";
                    headers["api-key"] = model.ApiKey;
                    break;
                case "minimax":
                case "minimax-chat":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/v1/models/list";
                    headers["Authorization"] = $"Bearer {model.ApiKey}";
                    break;
                case "gemini":
                    testUrl = $"https://generativelanguage.googleapis.com/v1/models";
                    break;
                case "qianfan":
                    testUrl = "https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/v Ernie-bot-turbo";
                    break;
                case "zhipu":
                    testUrl = "https://open.bigmodel.cn/api/paas/v4/models";
                    headers["Authorization"] = $"Bearer {model.ApiKey}";
                    break;
                case "other":
                default:
                    testUrl = model.ApiUrl;
                    break;
            }

            foreach (var header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await httpClient.GetAsync(testUrl);
            if (response.IsSuccessStatusCode)
            {
                return Result<string>.Ok("测试成功！模型连接正常。");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return Result<string>.Fail($"测试失败：{response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"测试失败：{ex.Message}");
        }
    }

    [HttpPost("generate/{articleId}")]
    [RequireRole("admin")]
    public async Task Generate(long articleId)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.Body.FlushAsync();

        try
        {
            var article = await _dbContext.Db.Queryable<Article>()
                .Where(it => it.Id == articleId)
                .FirstAsync();

            var articleContent = await _dbContext.ArticleContentDb
                .FirstAsync(it => it.ArticleId == articleId);

            if (article == null)
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "文章不存在" })}\n\n");
                return;
            }

            var content = "";
            if (articleContent != null && !string.IsNullOrWhiteSpace(articleContent.Content))
            {
                content = articleContent.Content;
            }
            else if (!string.IsNullOrWhiteSpace(article.Summary))
            {
                content = article.Summary;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "文章内容为空" })}\n\n");
                return;
            }

            var aiModel = await _aiModelService.GetDefaultAsync();

            if (aiModel == null || string.IsNullOrEmpty(aiModel.ApiKey))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "请先配置AI模型" })}\n\n");
                return;
            }

            await _aiSummaryService.GenerateSummaryStreamAsync(aiModel, article.Title, content, async (chunk) =>
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { content = chunk })}\n\n");
                await Response.Body.FlushAsync();
            });

            await _aiSummaryService.SaveSummaryAsync(articleId);

            // 重新缓存摘要
            var summary = await _aiSummaryService.GetByArticleIdAsync(articleId);
            if (summary != null)
            {
                _cache.Set($"ai_summary_{articleId}", summary, TimeSpan.FromHours(1));
            }

            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { done = true })}\n\n");
        }
        catch (Exception ex)
        {
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message })}\n\n");
        }
        finally
        {
            await Response.Body.FlushAsync();
        }
    }

    [HttpPost("generate/content")]
    [RequireRole("admin")]
    public async Task GenerateContent([FromBody] GenerateContentRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.Body.FlushAsync();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "请输入生成提示词" })}\n\n");
                return;
            }

            AiModelDto? aiModel = null;
            if (!string.IsNullOrEmpty(request.ServiceType) && !string.IsNullOrEmpty(request.ModelName))
            {
                aiModel = await _aiModelService.GetByTypeAndModelAsync(request.ServiceType, request.ModelName);
            }
            aiModel ??= await _aiModelService.GetDefaultAsync();

            if (aiModel == null || string.IsNullOrEmpty(aiModel.ApiKey))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = "请先配置AI模型" })}\n\n");
                return;
            }

            var systemPrompt = @"你是一位专业的内容写作助手，负责帮助用户撰写高质量的文章内容。
请根据用户提供的提示词，生成一篇结构完整、内容丰富的Markdown格式文章。
要求：
1. 文章包含合适的标题、段落结构
2. 内容详实、逻辑清晰
3. 使用Markdown格式正确排版
4. 直接输出文章内容，不需要任何前缀说明
5. 如果用户要求写代码，请确保代码块语法正确";

            await _aiSummaryService.GenerateContentStreamAsync(aiModel, systemPrompt, request.Prompt, request.Conversations, async (chunk) =>
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { content = chunk })}\n\n");
                await Response.Body.FlushAsync();
            });

            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { done = true })}\n\n");
        }
        catch (Exception ex)
        {
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message })}\n\n");
        }
        finally
        {
            await Response.Body.FlushAsync();
        }
    }
}

public class GenerateContentRequest
{
    public string Prompt { get; set; } = string.Empty;
    public List<ConversationMessage>? Conversations { get; set; }
    public string? ServiceType { get; set; }
    public string? ModelName { get; set; }
}

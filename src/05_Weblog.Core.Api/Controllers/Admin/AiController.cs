using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Service.AI;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Plugins;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/ai")]
[ApiController]
[Authorize]
[RequireRole("admin")]
public class AiController : ControllerBase
{
    private readonly IAiKernel _aiKernel;
    private readonly PluginManager _pluginManager;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiKernel aiKernel, PluginManager pluginManager, ILogger<AiController> logger)
    {
        _aiKernel = aiKernel;
        _pluginManager = pluginManager;
        _logger = logger;
    }

    [HttpGet("plugins")]
    public Result<List<AiPluginMetadata>> GetPlugins()
    {
        var plugins = _pluginManager.GetAllMetadata().ToList();
        return Result<List<AiPluginMetadata>>.Ok(plugins);
    }

    [HttpPost("plugin/{pluginId}/execute")]
    public async Task<Result<AiPluginResult>> ExecutePlugin(string pluginId, [FromBody] Dictionary<string, object> parameters)
    {
        try
        {
            var result = await _aiKernel.ExecutePluginAsync(pluginId, new AiPluginExecuteRequest
            {
                Parameters = parameters
            });
            return Result<AiPluginResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin execution failed: {PluginId}", pluginId);
            return Result<AiPluginResult>.Fail(ex.Message);
        }
    }

    [HttpPost("summary/generate/{articleId}")]
    public async Task<Result<string>> GenerateSummary(long articleId, [FromBody] SummaryRequest? request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.Content))
            {
                return Result<string>.Fail("文章内容不能为空");
            }

            var result = await _aiKernel.ExecutePluginAsync("article_summary", new AiPluginExecuteRequest
            {
                Parameters = new Dictionary<string, object>
                {
                    { "articleId", articleId },
                    { "content", request.Content },
                    { "model", request.Model ?? "gpt-4o-mini" }
                }
            });

            if (result.Success)
            {
                return Result<string>.Ok(result.Data ?? "");
            }
            return Result<string>.Fail(result.Error ?? "生成失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Summary generation failed");
            return Result<string>.Fail(ex.Message);
        }
    }

    [HttpPost("chat")]
    public async Task<Result<ChatResponse>> Chat([FromBody] ChatRequest? request)
    {
        try
        {
            _logger.LogInformation("Chat request received, Messages is null: {IsNull}, Count: {Count}", 
                request?.Messages == null, request?.Messages?.Count ?? -1);
            
            if (request?.Messages == null)
            {
                return Result<ChatResponse>.Fail("消息列表不能为空");
            }

            var messages = request.Messages.Select(m => new AiChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList();

            var result = await _aiKernel.ExecutePluginAsync("chat_assistant", new AiPluginExecuteRequest
            {
                SessionId = request.SessionId,
                Parameters = new Dictionary<string, object>
                {
                    { "messages", request.Messages },
                    { "model", request.Model ?? "gpt-4o-mini" },
                    { "stream", false }
                }
            });

            if (result.Success)
            {
                return Result<ChatResponse>.Ok(new ChatResponse
                {
                    Content = result.Data ?? "",
                    Model = result.Metadata.GetValueOrDefault("model")?.ToString() ?? "",
                    Provider = result.Metadata.GetValueOrDefault("provider")?.ToString() ?? ""
                });
            }
            return Result<ChatResponse>.Fail(result.Error ?? "对话失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat failed");
            return Result<ChatResponse>.Fail(ex.Message);
        }
    }

    [HttpPost("editor/assist")]
    public async Task<Result<string>> EditorAssist([FromBody] EditorAssistRequest request)
    {
        try
        {
            var result = await _aiKernel.EditorAssistAsync(
                request.CurrentContent,
                request.Instruction,
                request.Model
            );
            return Result<string>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Editor assist failed");
            return Result<string>.Fail(ex.Message);
        }
    }
}

public class SummaryRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    public string? Model { get; set; }
}

public class ChatRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("messages")]
    public List<ChatMessageItem> Messages { get; set; } = new();
    public string? SessionId { get; set; }
    public string? Model { get; set; }
}

public class ChatMessageItem
{
    [System.Text.Json.Serialization.JsonPropertyName("role")]
    public string Role { get; set; } = "user";
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class EditorAssistRequest
{
    public string CurrentContent { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string? Model { get; set; }
}
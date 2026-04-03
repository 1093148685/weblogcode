using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/ai/assistant")]
[ApiController]
[Authorize]
[RequireRole("admin")]
public class AiAssistantController : ControllerBase
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<AiAssistantController> _logger;
    private readonly IAiKernel _aiKernel;

    public AiAssistantController(ISqlSugarClient db, ILogger<AiAssistantController> logger, IAiKernel aiKernel)
    {
        _db = db;
        _logger = logger;
        _aiKernel = aiKernel;
    }

    [HttpGet("usage")]
    public async Task<Result<List<AiUsageStatDto>>> GetUsageStats()
    {
        try
        {
            var allLogs = await _db.Queryable<AiUsageLog>().ToListAsync();
            var today = DateTime.Now.Date;
            
            var stats = allLogs
                .GroupBy(l => l.UserId)
                .Select(g => new AiUsageStatDto
                {
                    UserId = g.Key,
                    UsedCount = g.FirstOrDefault(l => l.UsageDate.Date == today)?.UsageCount ?? 0,
                    LastUsedTime = g.Max(l => l.UpdatedAt).ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalCount = g.Sum(l => l.UsageCount)
                })
                .OrderByDescending(s => s.LastUsedTime)
                .ToList();

            return Result<List<AiUsageStatDto>>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage stats");
            return Result<List<AiUsageStatDto>>.Fail(ex.Message);
        }
    }

    [HttpGet("sessions")]
    public async Task<Result<List<AiSessionDto>>> GetSessions()
    {
        try
        {
            var sessions = await _db.Queryable<AiConversation>()
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            var sessionDtos = sessions.Select(s => new AiSessionDto
            {
                SessionId = s.SessionId,
                UserId = s.UserId ?? "anonymous",
                Title = GetSessionTitle(s.Messages),
                MessageCount = GetMessageCount(s.Messages),
                CreatedAt = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                LastMessageAt = s.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Messages = s.Messages
            }).ToList();

            return Result<List<AiSessionDto>>.Ok(sessionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions");
            return Result<List<AiSessionDto>>.Fail(ex.Message);
        }
    }

    [HttpDelete("session/{sessionId}")]
    public async Task<Result<bool>> DeleteSession(string sessionId)
    {
        try
        {
            var session = await _db.Queryable<AiConversation>()
                .FirstAsync(c => c.SessionId == sessionId);
            
            if (session != null)
            {
                await _db.Deleteable<AiConversation>().Where(c => c.SessionId == sessionId).ExecuteCommandAsync();
            }
            
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session: {SessionId}", sessionId);
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpGet("settings")]
    public async Task<Result<AiAssistantSettingsDto>> GetSettings()
    {
        try
        {
            var plugin = await _db.Queryable<AiPlugin>().FirstAsync(p => p.PluginId == "chat_assistant");
            
            var settings = new AiAssistantSettingsDto
            {
                DailyLimit = 10,
                Enabled = plugin?.IsEnabled ?? true
            };

            if (!string.IsNullOrEmpty(plugin?.Config))
            {
                try
                {
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(plugin.Config);
                    if (config != null)
                    {
                        if (config.TryGetValue("dailyLimit", out var limit))
                            settings.DailyLimit = Convert.ToInt32(limit);
                        if (config.TryGetValue("systemPrompt", out var prompt))
                            settings.SystemPrompt = prompt?.ToString() ?? "";
                        if (config.TryGetValue("temperature", out var temp))
                            settings.Temperature = Convert.ToSingle(temp);
                        if (config.TryGetValue("maxTokens", out var tokens))
                            settings.MaxTokens = Convert.ToInt32(tokens);
                    }
                }
                catch { }
            }

            return Result<AiAssistantSettingsDto>.Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings");
            return Result<AiAssistantSettingsDto>.Fail(ex.Message);
        }
    }

    [HttpPut("settings")]
    public async Task<Result<bool>> UpdateSettings([FromBody] AiAssistantSettingsDto settings)
    {
        try
        {
            var plugin = await _db.Queryable<AiPlugin>().FirstAsync(p => p.PluginId == "chat_assistant");
            
            if (plugin == null)
            {
                plugin = new AiPlugin
                {
                    PluginId = "chat_assistant",
                    Name = "AI 对话助手",
                    Description = "与 AI 进行对话",
                    IsEnabled = settings.Enabled,
                    Config = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        dailyLimit = settings.DailyLimit,
                        systemPrompt = settings.SystemPrompt,
                        temperature = settings.Temperature,
                        maxTokens = settings.MaxTokens
                    }),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                await _db.Insertable(plugin).ExecuteCommandAsync();
            }
            else
            {
                plugin.IsEnabled = settings.Enabled;
                plugin.Config = System.Text.Json.JsonSerializer.Serialize(new
                {
                    dailyLimit = settings.DailyLimit,
                    systemPrompt = settings.SystemPrompt,
                    temperature = settings.Temperature,
                    maxTokens = settings.MaxTokens
                });
                plugin.UpdatedAt = DateTime.Now;
                await _db.Updateable(plugin).ExecuteCommandAsync();
            }

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings");
            return Result<bool>.Fail(ex.Message);
        }
    }

    private string GetSessionTitle(string messagesJson)
    {
        try
        {
            if (string.IsNullOrEmpty(messagesJson)) return "新会话";
            var messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(messagesJson);
            var firstUserMessage = messages?.FirstOrDefault(m => m.Role == "user");
            if (firstUserMessage != null)
            {
                var content = firstUserMessage.Content;
                return content.Length > 30 ? content.Substring(0, 30) + "..." : content;
            }
        }
        catch { }
        return "新会话";
    }

    private int GetMessageCount(string messagesJson)
    {
        try
        {
            if (string.IsNullOrEmpty(messagesJson)) return 0;
            var messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(messagesJson);
            return messages?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    // ──────────────────── 写作 Plugin API ────────────────────

    [HttpPost("generate-article")]
    public async Task<Result<string>> GenerateArticle([FromBody] GenerateArticleRequest request)
    {
        try
        {
            var result = await _aiKernel.GenerateArticleAsync(
                request.Title, request.Outline, request.Style ?? "技术", request.WordCount > 0 ? request.WordCount : 800);
            return Result<string>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateArticle failed");
            return Result<string>.Fail(ex.Message);
        }
    }

    [HttpPost("seo-optimize")]
    public async Task<Result<string>> SeoOptimize([FromBody] SeoOptimizeRequest request)
    {
        try
        {
            var result = await _aiKernel.OptimizeSeoAsync(request.Title, request.Content, request.Keywords);
            return Result<string>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SeoOptimize failed");
            return Result<string>.Fail(ex.Message);
        }
    }

    [HttpPost("moderate")]
    public async Task<Result<string>> ModerateContent([FromBody] ModerateRequest request)
    {
        try
        {
            var result = await _aiKernel.ModerateContentAsync(request.Content);
            return Result<string>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ModerateContent failed");
            return Result<string>.Fail(ex.Message);
        }
    }

    // ──────────────────── Token 统计 ────────────────────

    [HttpGet("token-stats")]
    public async Task<Result<List<TokenStatDto>>> GetTokenStats([FromQuery] int days = 7)
    {
        try
        {
            var since = DateTime.Now.Date.AddDays(-days + 1);
            var logs = await _db.Queryable<AiUsageLog>()
                .Where(l => l.UsageDate >= since && l.TokensUsed != null)
                .ToListAsync();

            var stats = logs
                .GroupBy(l => new { Date = l.UsageDate.ToString("yyyy-MM-dd"), Provider = l.Provider ?? "unknown" })
                .Select(g => new TokenStatDto
                {
                    Date          = g.Key.Date,
                    Provider      = g.Key.Provider,
                    TotalTokens   = g.Sum(l => l.TokensUsed ?? 0),
                    TotalRequests = g.Sum(l => l.UsageCount)
                })
                .OrderByDescending(s => s.Date)
                .ToList();

            return Result<List<TokenStatDto>>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTokenStats failed");
            return Result<List<TokenStatDto>>.Fail(ex.Message);
        }
    }
}

public class AiUsageStatDto
{
    public string UserId { get; set; } = "";
    public int UsedCount { get; set; }
    public string LastUsedTime { get; set; } = "";
    public int TotalCount { get; set; }
}

public class AiSessionDto
{
    public string SessionId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public int MessageCount { get; set; }
    public string CreatedAt { get; set; } = "";
    public string LastMessageAt { get; set; } = "";
    public string Messages { get; set; } = "[]";
}

public class AiAssistantSettingsDto
{
    public int DailyLimit { get; set; } = 10;
    public bool Enabled { get; set; } = true;
    public string SystemPrompt { get; set; } = "";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 4096;
}

public class ChatMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

public class GenerateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Outline { get; set; }
    public string? Style { get; set; } = "技术";
    public int WordCount { get; set; } = 800;
}

public class SeoOptimizeRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Keywords { get; set; }
}

public class ModerateRequest
{
    public string Content { get; set; } = string.Empty;
}

public class TokenStatDto
{
    public string Date { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int TotalTokens { get; set; }
    public int TotalRequests { get; set; }
}
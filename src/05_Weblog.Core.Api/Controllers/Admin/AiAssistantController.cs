using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    public async Task<Result<object>> SeoOptimize([FromBody] SeoOptimizeRequest request)
    {
        try
        {
            var result = await _aiKernel.OptimizeSeoAsync(request.Title, request.Content, request.Keywords);
            return Result<object>.Ok(ParseSeoResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SeoOptimize failed");
            return Result<object>.Fail(ex.Message);
        }
    }

    [HttpPost("moderate")]
    public async Task<Result<object>> ModerateContent([FromBody] ModerateRequest request)
    {
        try
        {
            var result = await _aiKernel.ModerateContentAsync(request.Content);
            return Result<object>.Ok(ParseModerationResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ModerateContent failed");
            return Result<object>.Fail(ex.Message);
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

    private static object ParseAiJsonOrText(string raw, object fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```"))
        {
            trimmed = trimmed
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<object>(trimmed) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static SeoOptimizeResultDto ParseSeoResult(string raw)
    {
        var fallback = new SeoOptimizeResultDto
        {
            Score = 75,
            Suggestions = SplitSuggestionLines(raw),
            Raw = raw ?? ""
        };

        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var parsed = ParseAiJsonOrText(raw, fallback);
        if (parsed is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            return new SeoOptimizeResultDto
            {
                Score = ReadInt(element, "score", "Score") ?? fallback.Score,
                TitleSuggestion = ReadString(element, "titleSuggestion", "TitleSuggestion", "title", "Title") ?? "",
                MetaDescription = ReadString(element, "metaDescription", "MetaDescription", "description", "Description") ?? "",
                KeywordDensity = ReadString(element, "keywordDensity", "KeywordDensity") ?? "",
                Readability = ReadString(element, "readability", "Readability") ?? "",
                Keywords = ReadStringList(element, "keywords", "Keywords"),
                Suggestions = ReadStringList(element, "suggestions", "Suggestions"),
                Raw = raw
            };
        }

        var text = raw.Trim();
        fallback.TitleSuggestion = FirstNonEmpty(
            CaptureLine(text, @"(?:SEO\s*)?标题(?:建议)?\s*[:：]\s*(.+)"),
            SplitSuggestionLines(text).FirstOrDefault(line => line.Length <= 60));
        fallback.MetaDescription = CaptureLine(text, @"(?:meta\s*description|描述|摘要)\s*[:：]\s*(.+)") ?? "";
        fallback.Keywords = SplitKeywords(CaptureLine(text, @"(?:关键词|keywords)\s*[:：]\s*(.+)") ?? "");
        fallback.KeywordDensity = CaptureLine(text, @"关键词密度\s*[:：]\s*(.+)") ?? "";
        fallback.Readability = CaptureLine(text, @"可读性\s*[:：]\s*(.+)") ?? "";
        return fallback;
    }

    private static ModerationResultDto ParseModerationResult(string raw)
    {
        var fallback = new ModerationResultDto
        {
            Level = GuessModerationLevel(raw),
            Label = string.IsNullOrWhiteSpace(raw) ? "检测完成" : raw.Trim(),
            Issues = SplitSuggestionLines(raw),
            Raw = raw ?? ""
        };

        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var parsed = ParseAiJsonOrText(raw, fallback);
        if (parsed is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            return new ModerationResultDto
            {
                Level = ReadString(element, "level", "Level") ?? fallback.Level,
                Label = ReadString(element, "label", "Label", "summary", "Summary") ?? fallback.Label,
                Issues = ReadStringList(element, "issues", "Issues", "risks", "Risks"),
                Raw = raw
            };
        }

        return fallback;
    }

    private static string CleanAiJsonFence(string raw)
    {
        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("```")) return trimmed;

        return trimmed
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
        return null;
    }

    private static int? ReadInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
            if (int.TryParse(value.ToString(), out number)) return number;
        }
        return null;
    }

    private static List<string> ReadStringList(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.Array)
            {
                return value.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item!.Trim())
                    .ToList();
            }
            return SplitKeywords(value.ToString());
        }
        return new List<string>();
    }

    private static string? CaptureLine(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? CleanListMarker(match.Groups[1].Value) : null;
    }

    private static List<string> SplitSuggestionLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text.Split('\n')
            .Select(CleanListMarker)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(8)
            .ToList();
    }

    private static List<string> SplitKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return Regex.Split(text, @"[，,、;\s]+")
            .Select(CleanListMarker)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    private static string CleanListMarker(string? value)
    {
        return Regex.Replace(value ?? "", @"^\s*(?:[-*]|\d+[.、)])\s*", "")
            .Replace("**", "")
            .Trim();
    }

    private static string GuessModerationLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "safe";
        var text = raw.ToLowerInvariant();
        if (text.Contains("高危") || text.Contains("违规") || text.Contains("danger") || text.Contains("block"))
            return "danger";
        if (text.Contains("风险") || text.Contains("警告") || text.Contains("warning") || text.Contains("review"))
            return "warning";
        return "safe";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
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

public class SeoOptimizeResultDto
{
    public int Score { get; set; } = 75;
    public string TitleSuggestion { get; set; } = "";
    public string MetaDescription { get; set; } = "";
    public string KeywordDensity { get; set; } = "";
    public string Readability { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public string Raw { get; set; } = "";
}

public class ModerationResultDto
{
    public string Level { get; set; } = "safe";
    public string Label { get; set; } = "";
    public List<string> Issues { get; set; } = new();
    public string Raw { get; set; } = "";
}

public class TokenStatDto
{
    public string Date { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int TotalTokens { get; set; }
    public int TotalRequests { get; set; }
}

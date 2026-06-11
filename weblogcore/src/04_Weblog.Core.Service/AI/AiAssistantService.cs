using System.Text.Json;
using System.Text.RegularExpressions;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;

namespace Weblog.Core.Service.AI;

public interface IAiAssistantService
{
    Task<List<AiUsageStatDto>> GetUsageStatsAsync();
    Task<List<AiSessionDto>> GetSessionsAsync();
    Task<bool> DeleteSessionAsync(string sessionId);
    Task<AiAssistantSettingsDto> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(AiAssistantSettingsDto settings);
    Task<string> GenerateArticleAsync(GenerateArticleRequest request);
    Task<SeoOptimizeResultDto> OptimizeSeoAsync(SeoOptimizeRequest request);
    Task<ModerationResultDto> ModerateContentAsync(ModerateRequest request);
    Task<List<TokenStatDto>> GetTokenStatsAsync(int days);
}

public class AiAssistantService : IAiAssistantService
{
    private const string ChatAssistantPluginId = "chat_assistant";
    private const string DefaultSessionTitle = "新会话";
    private const string DefaultArticleStyle = "技术";
    private const int DefaultDailyLimit = 10;
    private const int DefaultWordCount = 800;
    private const int DefaultMaxTokens = 4096;
    private const float DefaultTemperature = 0.7f;

    private readonly ISqlSugarClient _db;
    private readonly IAiKernel _aiKernel;

    public AiAssistantService(ISqlSugarClient db, IAiKernel aiKernel)
    {
        _db = db;
        _aiKernel = aiKernel;
    }

    public async Task<List<AiUsageStatDto>> GetUsageStatsAsync()
    {
        var allLogs = await _db.Queryable<AiUsageLog>().ToListAsync();
        var today = DateTime.Now.Date;

        return allLogs
            .GroupBy(log => log.UserId)
            .Select(group => new AiUsageStatDto
            {
                UserId = group.Key,
                UsedCount = group.FirstOrDefault(log => log.UsageDate.Date == today)?.UsageCount ?? 0,
                LastUsedTime = group.Max(log => log.UpdatedAt).ToString("yyyy-MM-dd HH:mm:ss"),
                TotalCount = group.Sum(log => log.UsageCount)
            })
            .OrderByDescending(stat => stat.LastUsedTime)
            .ToList();
    }

    public async Task<List<AiSessionDto>> GetSessionsAsync()
    {
        var sessions = await _db.Queryable<AiConversation>()
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .ToListAsync();

        return sessions.Select(session => new AiSessionDto
        {
            SessionId = session.SessionId,
            UserId = string.IsNullOrWhiteSpace(session.UserId) ? "anonymous" : session.UserId,
            Title = GetSessionTitle(session.Messages),
            MessageCount = GetMessageCount(session.Messages),
            CreatedAt = session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            LastMessageAt = session.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Messages = session.Messages
        }).ToList();
    }

    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        await _db.Deleteable<AiConversation>()
            .Where(conversation => conversation.SessionId == sessionId)
            .ExecuteCommandAsync();

        return true;
    }

    public async Task<AiAssistantSettingsDto> GetSettingsAsync()
    {
        var plugin = await _db.Queryable<AiPlugin>()
            .FirstAsync(item => item.PluginId == ChatAssistantPluginId);

        var settings = new AiAssistantSettingsDto
        {
            DailyLimit = DefaultDailyLimit,
            Temperature = DefaultTemperature,
            MaxTokens = DefaultMaxTokens,
            Enabled = plugin?.IsEnabled ?? true
        };

        if (string.IsNullOrWhiteSpace(plugin?.Config))
            return settings;

        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(plugin.Config);
            if (config == null)
                return settings;

            settings.DailyLimit = ReadConfigInt(config, "dailyLimit", DefaultDailyLimit);
            settings.SystemPrompt = ReadConfigString(config, "systemPrompt", "");
            settings.Temperature = ReadConfigFloat(config, "temperature", DefaultTemperature);
            settings.MaxTokens = ReadConfigInt(config, "maxTokens", DefaultMaxTokens);
        }
        catch
        {
            return settings;
        }

        return NormalizeSettings(settings);
    }

    public async Task<bool> UpdateSettingsAsync(AiAssistantSettingsDto settings)
    {
        var normalized = NormalizeSettings(settings);
        var plugin = await _db.Queryable<AiPlugin>()
            .FirstAsync(item => item.PluginId == ChatAssistantPluginId);

        var now = DateTime.Now;
        var config = JsonSerializer.Serialize(new
        {
            dailyLimit = normalized.DailyLimit,
            systemPrompt = normalized.SystemPrompt,
            temperature = normalized.Temperature,
            maxTokens = normalized.MaxTokens
        });

        if (plugin == null)
        {
            plugin = new AiPlugin
            {
                PluginId = ChatAssistantPluginId,
                Name = "AI 对话助手",
                Description = "与 AI 进行对话",
                IsEnabled = normalized.Enabled,
                Config = config,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _db.Insertable(plugin).ExecuteCommandAsync();
            return true;
        }

        plugin.Name = string.IsNullOrWhiteSpace(plugin.Name) ? "AI 对话助手" : plugin.Name;
        plugin.Description = string.IsNullOrWhiteSpace(plugin.Description) ? "与 AI 进行对话" : plugin.Description;
        plugin.IsEnabled = normalized.Enabled;
        plugin.Config = config;
        plugin.UpdatedAt = now;

        await _db.Updateable(plugin).ExecuteCommandAsync();
        return true;
    }

    public Task<string> GenerateArticleAsync(GenerateArticleRequest request)
    {
        var title = request.Title?.Trim() ?? "";
        var outline = request.Outline?.Trim();
        var style = string.IsNullOrWhiteSpace(request.Style) ? DefaultArticleStyle : request.Style.Trim();
        var wordCount = request.WordCount > 0 ? request.WordCount : DefaultWordCount;

        return _aiKernel.GenerateArticleAsync(title, outline, style, wordCount);
    }

    public async Task<SeoOptimizeResultDto> OptimizeSeoAsync(SeoOptimizeRequest request)
    {
        var result = await _aiKernel.OptimizeSeoAsync(request.Title, request.Content, request.Keywords);
        return ParseSeoResult(result);
    }

    public async Task<ModerationResultDto> ModerateContentAsync(ModerateRequest request)
    {
        var result = await _aiKernel.ModerateContentAsync(request.Content);
        return ParseModerationResult(result);
    }

    public async Task<List<TokenStatDto>> GetTokenStatsAsync(int days)
    {
        var normalizedDays = Math.Clamp(days, 1, 90);
        var since = DateTime.Now.Date.AddDays(-normalizedDays + 1);
        var logs = await _db.Queryable<AiUsageLog>()
            .Where(log => log.UsageDate >= since && log.TokensUsed != null)
            .ToListAsync();

        return logs
            .GroupBy(log => new
            {
                Date = log.UsageDate.ToString("yyyy-MM-dd"),
                Provider = log.Provider ?? "unknown"
            })
            .Select(group => new TokenStatDto
            {
                Date = group.Key.Date,
                Provider = group.Key.Provider,
                TotalTokens = group.Sum(log => log.TokensUsed ?? 0),
                TotalRequests = group.Sum(log => log.UsageCount)
            })
            .OrderByDescending(stat => stat.Date)
            .ToList();
    }

    private static AiAssistantSettingsDto NormalizeSettings(AiAssistantSettingsDto settings)
    {
        settings.DailyLimit = settings.DailyLimit > 0 ? settings.DailyLimit : DefaultDailyLimit;
        settings.Temperature = settings.Temperature > 0 ? settings.Temperature : DefaultTemperature;
        settings.MaxTokens = settings.MaxTokens > 0 ? settings.MaxTokens : DefaultMaxTokens;
        settings.SystemPrompt ??= "";
        return settings;
    }

    private static string GetSessionTitle(string messagesJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messagesJson))
                return DefaultSessionTitle;

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(messagesJson);
            var firstUserMessage = messages?.FirstOrDefault(message => message.Role == "user");
            if (string.IsNullOrWhiteSpace(firstUserMessage?.Content))
                return DefaultSessionTitle;

            var content = firstUserMessage.Content.Trim();
            return content.Length > 30 ? content[..30] + "..." : content;
        }
        catch
        {
            return DefaultSessionTitle;
        }
    }

    private static int GetMessageCount(string messagesJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messagesJson))
                return 0;

            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(messagesJson);
            return messages?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int ReadConfigInt(Dictionary<string, JsonElement> config, string key, int fallback)
    {
        if (!config.TryGetValue(key, out var value))
            return fallback;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;

        return int.TryParse(value.ToString(), out number) ? number : fallback;
    }

    private static float ReadConfigFloat(Dictionary<string, JsonElement> config, string key, float fallback)
    {
        if (!config.TryGetValue(key, out var value))
            return fallback;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out var number))
            return number;

        return float.TryParse(value.ToString(), out number) ? number : fallback;
    }

    private static string ReadConfigString(Dictionary<string, JsonElement> config, string key, string fallback)
    {
        if (!config.TryGetValue(key, out var value))
            return fallback;

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : value.ToString();
    }

    private static object ParseAiJsonOrText(string raw, object fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        try
        {
            return JsonSerializer.Deserialize<object>(CleanAiJsonFence(raw)) ?? fallback;
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
        if (!trimmed.StartsWith("```"))
            return trimmed;

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
            if (!element.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;

            if (int.TryParse(value.ToString(), out number))
                return number;
        }

        return null;
    }

    private static List<string> ReadStringList(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
                continue;

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
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return text.Split('\n')
            .Select(CleanListMarker)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(8)
            .ToList();
    }

    private static List<string> SplitKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return Regex.Split(text, @"[,，、;\s]+")
            .Select(CleanListMarker)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    private static string CleanListMarker(string? value)
    {
        return Regex.Replace(value ?? "", @"^\s*(?:[-*]|\d+[.、])\s*", "")
            .Replace("**", "")
            .Trim();
    }

    private static string GuessModerationLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "safe";

        var text = raw.ToLowerInvariant();
        if (text.Contains("高危") || text.Contains("违规") || text.Contains("危险") || text.Contains("danger") || text.Contains("block"))
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

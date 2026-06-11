namespace Weblog.Core.Model.DTOs;

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

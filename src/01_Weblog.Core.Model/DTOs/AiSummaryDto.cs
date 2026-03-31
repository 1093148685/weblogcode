namespace Weblog.Core.Model.DTOs;

public class AiSummaryDto
{
    public long Id { get; set; }
    public long ArticleId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class CreateAiSummaryRequest
{
    public long ArticleId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class UpdateAiSummaryRequest
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

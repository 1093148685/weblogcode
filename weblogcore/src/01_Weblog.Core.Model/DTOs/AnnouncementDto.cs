namespace Weblog.Core.Model.DTOs;

public class AnnouncementDto
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class CreateAnnouncementRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class UpdateAnnouncementRequest
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class AnnouncementQueryRequest
{
    public string? Content { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

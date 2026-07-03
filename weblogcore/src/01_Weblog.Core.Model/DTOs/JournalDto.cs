namespace Weblog.Core.Model.DTOs;

public class JournalDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ReadNum { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class JournalAdminDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ReadNum { get; set; }
    public DateTime CreateTime { get; set; }
}

public class CreateJournalRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class UpdateJournalRequest
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class JournalPageRequest
{
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Date { get; set; }
}

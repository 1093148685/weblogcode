namespace Weblog.Core.Model.DTOs;

public class TagDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ArticlesTotal { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
}

public class UpdateTagRequest
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TagSelectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TagPageRequest : PageRequest
{
    public string? Keyword { get; set; }
    public string? Name { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

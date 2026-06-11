namespace Weblog.Core.Model.DTOs;

public class CategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ArticlesTotal { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CategorySelectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CategoryPageRequest : PageRequest
{
    public string? Name { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

namespace Weblog.Core.Model.DTOs;

public class WikiDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int Weight { get; set; }
    public bool IsTop => Weight > 0;
    public bool IsPublish { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public long? FirstArticleId { get; set; }
}

public class WikiCatalogDto
{
    public long Id { get; set; }
    public long WikiId { get; set; }
    public long? ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    public long? ParentId { get; set; }
    public int Sort { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public List<WikiCatalogDto>? Children { get; set; }
}

public class CreateWikiRequest
{
    public string Title { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int Weight { get; set; } = 0;
    public bool IsPublish { get; set; } = true;
}

public class UpdateWikiRequest
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int Weight { get; set; } = 0;
    public bool IsPublish { get; set; } = true;
}

public class CreateWikiCatalogRequest
{
    public long WikiId { get; set; }
    public long? ArticleId { get; set; }
    public long? ParentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Sort { get; set; } = 1;
}

public class UpdateWikiCatalogRequest
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Sort { get; set; } = 1;
}

public class BatchUpdateWikiCatalogRequest
{
    public long WikiId { get; set; }
    public List<WikiCatalogDto> Catalogs { get; set; } = new();
}

public class WikiPageRequest : PageRequest
{
    public string? Title { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>
/// 知识库文章上一篇/下一篇（返回目录节点信息）
/// </summary>
public class WikiPreNextDto
{
    public WikiCatalogDto? Pre { get; set; }
    public WikiCatalogDto? Next { get; set; }
}

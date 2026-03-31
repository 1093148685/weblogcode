using System.Text.Json.Serialization;

namespace Weblog.Core.Model.DTOs;

public class ArticleDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string Cover { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int Status { get; set; } = 1;
    public int ReadNum { get; set; }
    public int Weight { get; set; }
    public bool IsTop => Weight > 0;
    public int Type { get; set; } = 1;
    public DateTime CreateTime { get; set; }
    [JsonPropertyName("createDate")]
    public DateTime CreateDate => CreateTime;
    public DateTime UpdateTime { get; set; }
    public List<TagSelectDto>? Tags { get; set; }
    public List<long>? TagIds { get; set; }
    public CategorySimpleDto? Category { get; set; }
}

public class CategorySimpleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string Cover { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public List<long>? TagIds { get; set; }
    public List<long>? Tags { get; set; } // 兼容前端
    public int Status { get; set; } = 1;
    public int Weight { get; set; } = 0;
    public int Type { get; set; } = 1;
}

public class UpdateArticleRequest
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string Cover { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public List<long>? TagIds { get; set; }
    public List<long>? Tags { get; set; } // 兼容前端
    public int Status { get; set; } = 1;
    public int Weight { get; set; } = 0;
    public int Type { get; set; } = 1;
}

public class ArticleAdminDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Cover { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int Status { get; set; } = 1;
    public int ReadNum { get; set; }
    public int Weight { get; set; }
    public bool IsTop => Weight > 0;
    public int Type { get; set; } = 1;
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public List<TagSelectDto>? Tags { get; set; }
}

public class PreNextArticleDto
{
    public PreNextArticleItemDto? PreArticle { get; set; }
    public PreNextArticleItemDto? NextArticle { get; set; }
}

public class PreNextArticleItemDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class ArchiveArticleDto
{
    public string Month { get; set; } = string.Empty;
    public List<ArticleDto> Articles { get; set; } = new();
}

public class SearchArticleRequest
{
    public string? Keyword { get; set; }
    public string? Word { get; set; } // 兼容前端
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Current { get; set; } // 兼容前端
    public int Size { get; set; } // 兼容前端
}

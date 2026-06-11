namespace Weblog.Core.Model.DTOs;

public class PageDto<T>
{
    public List<T> List { get; set; } = new();
    public int Total { get; set; }
    public int PageNum { get; set; }
    public int PageSize { get; set; }
}

public class PageRequest
{
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class DashboardDto
{
    public int articleTotalCount { get; set; }
    public int categoryTotalCount { get; set; }
    public int tagTotalCount { get; set; }
    public long pvTotalCount { get; set; }
}

public class ArticlePublishTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ArticlePvTrendDto
{
    public string Date { get; set; } = string.Empty;
    public long ViewCount { get; set; }
}

public class StatisticsDto
{
    public long ArticleCount { get; set; }
    public long CategoryCount { get; set; }
    public long TagCount { get; set; }
    public long TotalPv { get; set; }
}

public class CategoryStatisticsDto
{
    public string Name { get; set; } = string.Empty;
    public long Count { get; set; }
}

public class TagStatisticsDto
{
    public string Name { get; set; } = string.Empty;
    public long Count { get; set; }
}

public class IdRequest
{
    public long Id { get; set; }
}

public class IdsRequest
{
    public List<long> Ids { get; set; } = new();
}

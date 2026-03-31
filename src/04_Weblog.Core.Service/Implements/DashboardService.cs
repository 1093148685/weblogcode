using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;
using SqlSugar;

namespace Weblog.Core.Service.Implements;

public class DashboardService : IDashboardService
{
    private readonly DbContext _dbContext;

    public DashboardService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        // 只统计未删除的文章
        var articleCount = await _dbContext.ArticleDb
            .Where(it => !it.IsDeleted)
            .CountAsync();

        // 只统计未删除的分类
        var categoryCount = await _dbContext.CategoryDb
            .Where(it => !it.IsDeleted)
            .CountAsync();

        // 只统计未删除的标签
        var tagCount = await _dbContext.TagDb
            .Where(it => !it.IsDeleted)
            .CountAsync();

        // 总 PV 统计
        var totalPv = await _dbContext.StatisticsDb
            .SumAsync(it => it.ViewCount);

        return new DashboardDto
        {
            articleTotalCount = articleCount,
            categoryTotalCount = categoryCount,
            tagTotalCount = tagCount,
            pvTotalCount = totalPv
        };
    }

    public async Task<List<ArticlePublishTrendDto>> GetArticlePublishTrendAsync(int months = 6)
    {
        var startDate = DateTime.Now.AddMonths(-months);
        var articles = await _dbContext.ArticleDb
            .Where(it => !it.IsDeleted && it.CreateTime >= startDate)
            .ToListAsync();

        // 在内存中按年月分组统计
        var grouped = articles
            .GroupBy(it => it.CreateTime.ToString("yyyy-MM"))
            .Select(g => new ArticlePublishTrendDto
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(it => it.Date)
            .ToList();

        return grouped;
    }

    public async Task<List<ArticlePvTrendDto>> GetArticlePvTrendAsync(int days = 7)
    {
        var endDate = DateTime.Now;
        var startDate = endDate.AddDays(-(days - 1));
        var startDateStr = startDate.ToString("yyyy-MM-dd");
        var endDateStr = endDate.ToString("yyyy-MM-dd");

        var statsList = await _dbContext.StatisticsDb
            .Where(it => it.Date.CompareTo(startDateStr) >= 0)
            .Where(it => it.Date.CompareTo(endDateStr) <= 0)
            .ToListAsync();

        var statsDict = statsList.ToDictionary(it => it.Date);

        var result = new List<ArticlePvTrendDto>();
        for (int i = days - 1; i >= 0; i--)
        {
            var date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
            var hasStats = statsDict.TryGetValue(date, out var stats);

            result.Add(new ArticlePvTrendDto
            {
                Date = date,
                ViewCount = hasStats ? stats.ViewCount : 0
            });
        }

        return result;
    }

    public async Task IncrementPvAsync()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var stats = await _dbContext.StatisticsDb.FirstAsync(it => it.Date == today);

        if (stats == null)
        {
            stats = new Statistics
            {
                Date = today,
                ViewCount = 1
            };
            await _dbContext.Db.Insertable(stats).ExecuteCommandAsync();
        }
        else
        {
            await _dbContext.Db.Updateable<Statistics>()
                .SetColumns(it => it.ViewCount == it.ViewCount + 1)
                .Where(it => it.Date == today)
                .ExecuteCommandAsync();
        }
    }

    public async Task<StatisticsDto> GetStatisticsInfoAsync()
    {
        var articleCount = await _dbContext.ArticleDb
            .Where(it => !it.IsDeleted && it.Status == 1)
            .CountAsync();

        var categoryCount = await _dbContext.CategoryDb
            .Where(it => !it.IsDeleted)
            .CountAsync();

        var tagCount = await _dbContext.TagDb
            .Where(it => !it.IsDeleted)
            .CountAsync();

        var totalPv = await _dbContext.StatisticsDb
            .SumAsync(it => it.ViewCount);

        return new StatisticsDto
        {
            ArticleCount = articleCount,
            CategoryCount = categoryCount,
            TagCount = tagCount,
            TotalPv = totalPv
        };
    }

    public async Task<List<CategoryStatisticsDto>> GetCategoryStatisticsAsync()
    {
        // 单次 JOIN + GROUP BY 查询，避免全表加载到内存
        // 使用 MergeTable() 将多表 Select 结果合并为单表，解决别名不一致问题
        var result = await _dbContext.Db.Queryable<ArticleCategoryRel>()
            .InnerJoin<Category>((rel, c) => rel.CategoryId == c.Id && !c.IsDeleted)
            .GroupBy((rel, c) => new { c.Id, c.Name })
            .Select((rel, c) => new CategoryStatisticsDto
            {
                Name = c.Name,
                Count = SqlFunc.AggregateCount(rel.Id)
            })
            .MergeTable()
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        return result;
    }

    public async Task<List<TagStatisticsDto>> GetTagStatisticsAsync()
    {
        // 单次 JOIN + GROUP BY 查询，避免全表加载到内存
        // 使用 MergeTable() 将多表 Select 结果合并为单表，解决别名不一致问题
        var result = await _dbContext.Db.Queryable<ArticleTag>()
            .InnerJoin<Tag>((at, t) => at.TagId == t.Id && !t.IsDeleted)
            .GroupBy((at, t) => new { t.Id, t.Name })
            .Select((at, t) => new TagStatisticsDto
            {
                Name = t.Name,
                Count = SqlFunc.AggregateCount(at.Id)
            })
            .MergeTable()
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .Take(15)
            .ToListAsync();

        return result;
    }
}

using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<List<ArticlePublishTrendDto>> GetArticlePublishTrendAsync(int months = 6);
    Task<List<ArticlePvTrendDto>> GetArticlePvTrendAsync(int days = 7);
    Task IncrementPvAsync();
    Task<StatisticsDto> GetStatisticsInfoAsync();
    Task<List<CategoryStatisticsDto>> GetCategoryStatisticsAsync();
    Task<List<TagStatisticsDto>> GetTagStatisticsAsync();
}

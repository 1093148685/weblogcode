using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/dashboard")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpPost("statistics")]
    public async Task<Result<DashboardDto>> GetDashboard()
    {
        var result = await _dashboardService.GetDashboardAsync();
        return Result<DashboardDto>.Ok(result);
    }

    [HttpPost("publishArticle/statistics")]
    public async Task<Result<List<ArticlePublishTrendDto>>> GetPublishTrend([FromBody] DashboardRequest request)
    {
        var result = await _dashboardService.GetArticlePublishTrendAsync(request.Months);
        return Result<List<ArticlePublishTrendDto>>.Ok(result);
    }

    [HttpPost("pv/statistics")]
    public async Task<Result<List<ArticlePvTrendDto>>> GetPvTrend([FromBody] PvTrendRequest request)
    {
        var result = await _dashboardService.GetArticlePvTrendAsync(request.Days);
        return Result<List<ArticlePvTrendDto>>.Ok(result);
    }

    [HttpPost("category/statistics")]
    public async Task<Result<List<CategoryStatisticsDto>>> GetCategoryStatistics()
    {
        var result = await _dashboardService.GetCategoryStatisticsAsync();
        return Result<List<CategoryStatisticsDto>>.Ok(result);
    }

    [HttpPost("tag/statistics")]
    public async Task<Result<List<TagStatisticsDto>>> GetTagStatistics()
    {
        var result = await _dashboardService.GetTagStatisticsAsync();
        return Result<List<TagStatisticsDto>>.Ok(result);
    }
}

public class DashboardRequest
{
    public int Months { get; set; } = 6;
}

public class PvTrendRequest
{
    public int Days { get; set; } = 7;
}

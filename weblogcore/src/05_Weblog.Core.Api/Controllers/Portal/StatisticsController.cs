using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/statistics")]
[ApiController]
[OutputCache]
public class StatisticsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public StatisticsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpPost("info")]
    public async Task<Result<StatisticsDto>> GetStatisticsInfo()
    {
        var result = await _dashboardService.GetStatisticsInfoAsync();
        return Result<StatisticsDto>.Ok(result);
    }
}

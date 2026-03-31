using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/article")]
[ApiController]
[OutputCache]
public class ArticlePortalController : ControllerBase
{
    private readonly IArticlePortalService _articlePortalService;
    private readonly IDashboardService _dashboardService;

    public ArticlePortalController(IArticlePortalService articlePortalService, IDashboardService dashboardService)
    {
        _articlePortalService = articlePortalService;
        _dashboardService = dashboardService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<ArticleDto>>> GetPage([FromBody] PageRequest request)
    {
        var result = await _articlePortalService.GetPageAsync(request);
        return Result<PageDto<ArticleDto>>.Ok(result);
    }

    [HttpPost("detail")]
    public async Task<Result<ArticleDto>> GetById([FromBody] ArticleDetailRequest request)
    {
        var result = await _articlePortalService.GetByIdAsync(request.ArticleId);

        _ = Task.Run(async () =>
        {
            await _articlePortalService.IncrementViewCountAsync(request.ArticleId);
            await _dashboardService.IncrementPvAsync();
        });

        return Result<ArticleDto>.Ok(result);
    }

    [HttpPost("preNext")]
    public async Task<Result<PreNextArticleDto>> GetPreNextArticle([FromBody] ArticleDetailRequest request)
    {
        var result = await _articlePortalService.GetPreNextArticleAsync(request.ArticleId);
        return Result<PreNextArticleDto>.Ok(result);
    }
}

public class ArticleDetailRequest
{
    public long ArticleId { get; set; }
}

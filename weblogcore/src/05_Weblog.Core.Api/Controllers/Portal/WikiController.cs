using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[ApiController]
[Route("api/wiki")]
[OutputCache]
public class WikiPortalController : ControllerBase
{
    private readonly IWikiService _wikiService;

    public WikiPortalController(IWikiService wikiService)
    {
        _wikiService = wikiService;
    }

    [HttpPost("list")]
    public async Task<Result<List<WikiDto>>> GetPublishList()
    {
        var result = await _wikiService.GetPublishListAsync();
        return Result<List<WikiDto>>.Ok(result);
    }

    [HttpPost("detail")]
    public async Task<Result<WikiDto>> GetById([FromBody] WikiDetailRequest request)
    {
        var result = await _wikiService.GetByIdAsync(request.Id);
        return Result<WikiDto>.Ok(result);
    }

    [HttpPost("catalog/list")]
    public async Task<Result<List<WikiCatalogDto>>> GetCatalogs([FromBody] WikiCatalogRequest request)
    {
        var result = await _wikiService.GetCatalogsAsync(request.Id);
        return Result<List<WikiCatalogDto>>.Ok(result);
    }

    [HttpPost("article/preNext")]
    public async Task<Result<WikiPreNextDto>> GetWikiArticlePreNext([FromBody] WikiPreNextRequest request)
    {
        var result = await _wikiService.GetPreNextArticleAsync(request.WikiId, request.ArticleId);
        return Result<WikiPreNextDto>.Ok(result);
    }
}

public class WikiDetailRequest
{
    public long Id { get; set; }
}

public class WikiCatalogRequest
{
    public long Id { get; set; }
}

public class WikiPreNextRequest
{
    public long WikiId { get; set; }
    public long ArticleId { get; set; }
}

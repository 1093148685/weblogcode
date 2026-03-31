using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/wiki")]
[Authorize]
public class WikiController : ControllerBase
{
    private readonly IWikiService _wikiService;

    public WikiController(IWikiService wikiService)
    {
        _wikiService = wikiService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<WikiDto>>> GetPage([FromBody] WikiPageRequest request)
    {
        var result = await _wikiService.GetAdminPageAsync(request);
        return Result<PageDto<WikiDto>>.Ok(result);
    }

    [HttpPost("add")]
    [RequireRole("admin")]
    public async Task<Result<WikiDto>> Create([FromBody] CreateWikiRequest request)
    {
        var result = await _wikiService.CreateAsync(request);
        return Result<WikiDto>.Ok(result);
    }

    [HttpPost("update")]
    [RequireRole("admin")]
    public async Task<Result<WikiDto>> Update([FromBody] UpdateWikiRequest request)
    {
        var result = await _wikiService.UpdateAsync(request);
        return Result<WikiDto>.Ok(result);
    }

    [HttpPost("delete")]
    [RequireRole("admin")]
    public async Task<Result> Delete([FromBody] IdRequest request)
    {
        var result = await _wikiService.DeleteAsync(request.Id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }

    [HttpPost("isTop/update")]
    [RequireRole("admin")]
    public async Task<Result> UpdateIsTop([FromBody] UpdateIsTopRequest request)
    {
        var result = await _wikiService.UpdateIsTopAsync(request.Id, request.IsTop);
        return result ? Result.Ok() : Result.Fail("更新失败");
    }

    [HttpPost("isPublish/update")]
    [RequireRole("admin")]
    public async Task<Result> UpdateIsPublish([FromBody] UpdateIsPublishRequest request)
    {
        var result = await _wikiService.UpdateIsPublishAsync(request.Id, request.IsPublish);
        return result ? Result.Ok() : Result.Fail("更新失败");
    }

    // 目录管理
    [HttpPost("catalog/list")]
    public async Task<Result<List<WikiCatalogDto>>> GetCatalogs([FromBody] IdRequest request)
    {
        var result = await _wikiService.GetCatalogsAsync(request.Id);
        return Result<List<WikiCatalogDto>>.Ok(result);
    }

    [HttpPost("catalog/add")]
    [RequireRole("admin")]
    public async Task<Result<WikiCatalogDto>> CreateCatalog([FromBody] CreateWikiCatalogRequest request)
    {
        var result = await _wikiService.CreateCatalogAsync(request);
        return Result<WikiCatalogDto>.Ok(result);
    }

    [HttpPost("catalog/update")]
    [RequireRole("admin")]
    public async Task<Result<WikiCatalogDto>> UpdateCatalog([FromBody] UpdateWikiCatalogRequest request)
    {
        var result = await _wikiService.UpdateCatalogAsync(request);
        return Result<WikiCatalogDto>.Ok(result);
    }

    [HttpPost("catalog/batchUpdate")]
    [RequireRole("admin")]
    public async Task<Result<List<WikiCatalogDto>>> BatchUpdateCatalogs([FromBody] BatchUpdateWikiCatalogRequest request)
    {
        var result = await _wikiService.BatchUpdateCatalogsAsync(request);
        return Result<List<WikiCatalogDto>>.Ok(result);
    }

    [HttpPost("catalog/delete")]
    [RequireRole("admin")]
    public async Task<Result> DeleteCatalog([FromBody] IdRequest request)
    {
        var result = await _wikiService.DeleteCatalogAsync(request.Id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }
}

public class UpdateIsPublishRequest
{
    public long Id { get; set; }
    public bool IsPublish { get; set; }
}

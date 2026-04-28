using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/article")]
[ApiController]
[Authorize]
public class ArticleController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticleController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<ArticleAdminDto>>> GetPage([FromBody] PageRequest request)
    {
        var result = await _articleService.GetAdminPageAsync(request);
        return Result<PageDto<ArticleAdminDto>>.Ok(result);
    }

    [HttpPost("detail")]
    public async Task<Result<ArticleDto>> GetById([FromBody] IdRequest request)
    {
        var result = await _articleService.GetByIdAsync(request.Id);
        return Result<ArticleDto>.Ok(result);
    }

    [HttpPost("publish")]
    [RequireRole("admin")]
    public async Task<Result<ArticleDto>> Publish([FromBody] CreateArticleRequest request)
    {
        request.Status = 1; // 已发布
        var result = await _articleService.CreateAsync(request);
        return Result<ArticleDto>.Ok(result);
    }

    [HttpPost("update")]
    [RequireRole("admin")]
    public async Task<Result<ArticleDto>> Update([FromBody] UpdateArticleRequest request)
    {
        var result = await _articleService.UpdateAsync(request);
        return Result<ArticleDto>.Ok(result);
    }

    [HttpPost("delete")]
    [RequireRole("admin")]
    public async Task<Result> Delete([FromBody] IdRequest request)
    {
        var result = await _articleService.DeleteAsync(request.Id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }

    [HttpPost("isTop/update")]
    [RequireRole("admin")]
    public async Task<Result> UpdateIsTop([FromBody] UpdateIsTopRequest request)
    {
        var result = await _articleService.UpdateIsTopAsync(request.Id, request.IsTop);
        return result ? Result.Ok() : Result.Fail("更新失败");
    }

    [HttpPost("status/update")]
    [RequireRole("admin")]
    public async Task<Result> UpdateStatus([FromBody] UpdateArticleStatusRequest request)
    {
        var result = await _articleService.UpdateStatusAsync(request.Id, request.Status);
        return result ? Result.Ok() : Result.Fail("更新失败");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/blog/settings")]
[ApiController]
[Authorize]
public class BlogSettingsController : ControllerBase
{
    private readonly IBlogSettingsService _blogSettingsService;

    public BlogSettingsController(IBlogSettingsService blogSettingsService)
    {
        _blogSettingsService = blogSettingsService;
    }

    [HttpPost("detail")]
    public async Task<Result<BlogSettingsDto>> Get()
    {
        var result = await _blogSettingsService.GetAsync();
        return Result<BlogSettingsDto>.Ok(result);
    }

    [HttpPost("update")]
    [RequireRole("admin")]
    public async Task<Result<BlogSettingsDto>> Update([FromBody] UpdateBlogSettingsRequest request)
    {
        var result = await _blogSettingsService.UpdateAsync(request);
        return Result<BlogSettingsDto>.Ok(result);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/blog/settings")]
[ApiController]
[OutputCache]
public class BlogSettingsPortalController : ControllerBase
{
    private readonly IBlogSettingsService _blogSettingsService;

    public BlogSettingsPortalController(IBlogSettingsService blogSettingsService)
    {
        _blogSettingsService = blogSettingsService;
    }

    [HttpPost("detail")]
    public async Task<Result<BlogSettingsDto>> Get()
    {
        var result = await _blogSettingsService.GetAsync();
        return Result<BlogSettingsDto>.Ok(result);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/archive")]
[ApiController]
[OutputCache]
public class ArchiveController : ControllerBase
{
    private readonly IArticlePortalService _articlePortalService;

    public ArchiveController(IArticlePortalService articlePortalService)
    {
        _articlePortalService = articlePortalService;
    }

    [HttpPost("list")]
    [OutputCache(Duration = 300)]
    public async Task<Result<List<ArchiveArticleDto>>> GetArchiveList([FromBody] ArchiveListRequest? request)
    {
        var result = await _articlePortalService.GetArchiveListAsync(request?.Size ?? 50);
        return Result<List<ArchiveArticleDto>>.Ok(result);
    }
}

public class ArchiveListRequest
{
    public int? Size { get; set; }
}

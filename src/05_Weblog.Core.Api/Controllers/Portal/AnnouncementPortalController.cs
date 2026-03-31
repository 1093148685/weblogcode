using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/announcement")]
[ApiController]
[OutputCache]
public class AnnouncementPortalController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementPortalController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    [HttpGet]
    public async Task<Result<AnnouncementDto>> Get()
    {
        var result = await _announcementService.GetAsync();
        return Result<AnnouncementDto>.Ok(result);
    }
}

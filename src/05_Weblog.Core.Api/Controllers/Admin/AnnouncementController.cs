using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/announcement")]
[ApiController]
[Authorize]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    [HttpGet]
    public async Task<Result<AnnouncementDto>> Get()
    {
        var result = await _announcementService.GetAsync();
        return Result<AnnouncementDto>.Ok(result);
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<AnnouncementDto>>> Query([FromBody] AnnouncementQueryRequest request)
    {
        var (list, total) = await _announcementService.QueryAsync(request);
        return Result<PageDto<AnnouncementDto>>.Ok(new PageDto<AnnouncementDto>
        {
            List = list,
            Total = (int)total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        });
    }

    [HttpDelete("{id}")]
    [RequireRole("admin")]
    public async Task<Result> Delete(long id)
    {
        var result = await _announcementService.DeleteAsync(id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }

    [HttpPost]
    [RequireRole("admin")]
    public async Task<Result<AnnouncementDto>> Create([FromBody] CreateAnnouncementRequest request)
    {
        var result = await _announcementService.CreateAsync(request);
        return Result<AnnouncementDto>.Ok(result);
    }

    [HttpPut]
    [RequireRole("admin")]
    public async Task<Result<AnnouncementDto>> Update([FromBody] UpdateAnnouncementRequest request)
    {
        var result = await _announcementService.UpdateAsync(request);
        return Result<AnnouncementDto>.Ok(result);
    }
}

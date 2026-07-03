using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/journal")]
[ApiController]
[Authorize]
public class JournalController : ControllerBase
{
    private readonly IJournalService _journalService;

    public JournalController(IJournalService journalService)
    {
        _journalService = journalService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<JournalAdminDto>>> GetPage([FromBody] PageRequest request)
    {
        var result = await _journalService.GetAdminPageAsync(request);
        return Result<PageDto<JournalAdminDto>>.Ok(result);
    }

    [HttpPost("create")]
    [RequireRole("admin")]
    public async Task<Result<JournalDto>> Create([FromBody] CreateJournalRequest request)
    {
        var result = await _journalService.CreateAsync(request);
        return Result<JournalDto>.Ok(result);
    }

    [HttpPost("update")]
    [RequireRole("admin")]
    public async Task<Result<JournalDto>> Update([FromBody] UpdateJournalRequest request)
    {
        var result = await _journalService.UpdateAsync(request);
        return Result<JournalDto>.Ok(result);
    }

    [HttpPost("delete")]
    [RequireRole("admin")]
    public async Task<Result> Delete([FromBody] IdRequest request)
    {
        var result = await _journalService.DeleteAsync(request.Id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }

    [HttpPost("detail")]
    public async Task<Result<JournalDto>> GetById([FromBody] IdRequest request)
    {
        var result = await _journalService.GetByIdAsync(request.Id);
        return Result<JournalDto>.Ok(result);
    }
}

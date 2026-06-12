using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/journal")]
[ApiController]
[OutputCache]
public class JournalPortalController : ControllerBase
{
    private readonly IJournalPortalService _journalPortalService;

    public JournalPortalController(IJournalPortalService journalPortalService)
    {
        _journalPortalService = journalPortalService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<JournalDto>>> GetPage([FromBody] JournalPageRequest request)
    {
        var result = await _journalPortalService.GetPageAsync(request);
        return Result<PageDto<JournalDto>>.Ok(result);
    }

    [HttpPost("detail")]
    public async Task<Result<JournalDto>> GetById([FromBody] JournalDetailRequest request)
    {
        var result = await _journalPortalService.GetByIdAsync(request.JournalId);

        // 异步增加阅读量
        _ = Task.Run(async () =>
        {
            await _journalPortalService.IncrementViewCountAsync(request.JournalId);
        });

        return Result<JournalDto>.Ok(result);
    }

    [HttpPost("dates")]
    public async Task<Result<List<string>>> GetDatesWithJournals()
    {
        var result = await _journalPortalService.GetDatesWithJournalsAsync();
        return Result<List<string>>.Ok(result);
    }
}

public class JournalDetailRequest
{
    public long JournalId { get; set; }
}

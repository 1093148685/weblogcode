using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IJournalPortalService
{
    Task<PageDto<JournalDto>> GetPageAsync(JournalPageRequest request);
    Task<JournalDto> GetByIdAsync(long id);
    Task IncrementViewCountAsync(long id);
    Task<List<string>> GetDatesWithJournalsAsync();
}

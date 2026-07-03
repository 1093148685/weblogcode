using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IJournalService
{
    Task<JournalDto> CreateAsync(CreateJournalRequest request);
    Task<JournalDto> UpdateAsync(UpdateJournalRequest request);
    Task<bool> DeleteAsync(long id);
    Task<JournalDto> GetByIdAsync(long id);
    Task<PageDto<JournalAdminDto>> GetAdminPageAsync(PageRequest request);
}

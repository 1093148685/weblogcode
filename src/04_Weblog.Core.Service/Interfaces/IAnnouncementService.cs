using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IAnnouncementService
{
    Task<AnnouncementDto> GetAsync();
    Task<AnnouncementDto> CreateAsync(CreateAnnouncementRequest request);
    Task<AnnouncementDto> UpdateAsync(UpdateAnnouncementRequest request);
    Task<(List<AnnouncementDto> list, long total)> QueryAsync(AnnouncementQueryRequest request);
    Task<bool> DeleteAsync(long id);
}

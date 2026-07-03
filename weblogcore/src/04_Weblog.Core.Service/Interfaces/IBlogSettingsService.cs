using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IBlogSettingsService
{
    Task<BlogSettingsDto> GetAsync();
    Task<BlogSettingsDto> UpdateAsync(UpdateBlogSettingsRequest request);
    Task<string> UploadImageAsync(string folder, string fileName, byte[] fileData);
}

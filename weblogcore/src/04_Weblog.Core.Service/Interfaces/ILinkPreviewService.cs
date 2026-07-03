using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface ILinkPreviewService
{
    Task<LinkPreviewDto?> GetPreviewAsync(string url);
    bool IsUrlAllowed(string url);
    Task<LinkPreviewDto?> FetchAndCacheAsync(string url);
    Task ClearCacheAsync();
}
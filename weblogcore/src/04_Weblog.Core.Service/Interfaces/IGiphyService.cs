using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IGiphyService
{
    Task<List<GiphyItemDto>> SearchAsync(string query, int limit = 20, int offset = 0);
    Task<List<GiphyItemDto>> TrendingAsync(int limit = 20, int offset = 0);
}
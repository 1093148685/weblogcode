using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface ITagService
{
    Task<TagDto> CreateAsync(CreateTagRequest request);
    Task<TagDto> UpdateAsync(UpdateTagRequest request);
    Task<bool> DeleteAsync(long id);
    Task<PageDto<TagDto>> GetPageAsync(TagPageRequest request);
    Task<List<TagSelectDto>> GetSelectListAsync();
    Task<List<TagSelectDto>> SearchSelectListAsync(string keyword);
    Task<bool> HasArticleAsync(long id);
    Task<List<TagDto>> GetListAsync(int? size = null);
}

using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateAsync(UpdateCategoryRequest request);
    Task<bool> DeleteAsync(long id);
    Task<PageDto<CategoryDto>> GetPageAsync(CategoryPageRequest request);
    Task<List<CategorySelectDto>> GetSelectListAsync();
    Task<bool> HasArticleAsync(long id);
    Task<List<CategoryDto>> GetListAsync(int? size = null);
}

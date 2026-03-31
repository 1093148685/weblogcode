using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IArticleService
{
    Task<ArticleDto> CreateAsync(CreateArticleRequest request);
    Task<ArticleDto> UpdateAsync(UpdateArticleRequest request);
    Task<bool> DeleteAsync(long id);
    Task<ArticleDto> GetByIdAsync(long id);
    Task<PageDto<ArticleAdminDto>> GetAdminPageAsync(PageRequest request);
    Task<List<TagSelectDto>> GetAllTagsAsync();
    Task<bool> UpdateIsTopAsync(long id, bool isTop);
}

public interface IArticlePortalService
{
    Task<PageDto<ArticleDto>> GetPageAsync(PageRequest request);
    Task<PageDto<ArticleDto>> GetArchivePageAsync(PageRequest request);
    Task<List<ArchiveArticleDto>> GetArchiveListAsync();
    Task<PageDto<ArticleDto>> GetPageByCategoryAsync(long categoryId, PageRequest request);
    Task<PageDto<ArticleDto>> GetPageByTagAsync(long tagId, PageRequest request);
    Task<PageDto<ArticleDto>> GetPageByKeywordAsync(string keyword, PageRequest request);
    Task<ArticleDto> GetByIdAsync(long id);
    Task IncrementViewCountAsync(long id);
    Task<PreNextArticleDto> GetPreNextArticleAsync(long articleId);
}

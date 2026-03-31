using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IWikiService
{
    // Wiki管理
    Task<WikiDto> CreateAsync(CreateWikiRequest request);
    Task<WikiDto> UpdateAsync(UpdateWikiRequest request);
    Task<bool> DeleteAsync(long id);
    Task<WikiDto> GetByIdAsync(long id);
    Task<PageDto<WikiDto>> GetAdminPageAsync(WikiPageRequest request);
    Task<List<WikiDto>> GetPublishListAsync();
    Task<bool> UpdateIsTopAsync(long id, bool isTop);
    Task<bool> UpdateIsPublishAsync(long id, bool isPublish);
    Task<WikiPreNextDto> GetPreNextArticleAsync(long wikiId, long articleId);

    // Wiki目录管理
    Task<WikiCatalogDto> CreateCatalogAsync(CreateWikiCatalogRequest request);
    Task<WikiCatalogDto> UpdateCatalogAsync(UpdateWikiCatalogRequest request);
    Task<bool> DeleteCatalogAsync(long id);
    Task<List<WikiCatalogDto>> GetCatalogsAsync(long wikiId);
    Task<List<WikiCatalogDto>> BatchUpdateCatalogsAsync(BatchUpdateWikiCatalogRequest request);
}

using Mapster;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class WikiService : IWikiService
{
    private readonly DbContext _dbContext;

    public WikiService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region Wiki管理

    public async Task<WikiDto> CreateAsync(CreateWikiRequest request)
    {
        var wiki = new Wiki
        {
            Title = request.Title,
            Cover = request.Cover,
            Summary = request.Summary,
            Weight = request.Weight,
            IsPublish = request.IsPublish,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(wiki).ExecuteReturnIdentityAsync();
        wiki.Id = id;

        return wiki.Adapt<WikiDto>();
    }

    public async Task<WikiDto> UpdateAsync(UpdateWikiRequest request)
    {
        var wiki = await _dbContext.WikiDb
            .Where(it => it.Id == request.Id && !it.IsDeleted)
            .FirstAsync();
        if (wiki == null)
        {
            throw new Exception("知识库不存在");
        }

        wiki.Title = request.Title;
        wiki.Cover = request.Cover;
        wiki.Summary = request.Summary;
        wiki.Weight = request.Weight;
        wiki.IsPublish = request.IsPublish;
        wiki.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(wiki).ExecuteCommandAsync();

        return wiki.Adapt<WikiDto>();
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var wiki = await _dbContext.WikiDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (wiki == null)
        {
            return false;
        }

        // 逻辑删除关联目录
        var catalogs = await _dbContext.WikiCatalogDb
            .Where(it => it.WikiId == id && !it.IsDeleted)
            .ToListAsync();
        foreach (var catalog in catalogs)
        {
            catalog.IsDeleted = true;
            catalog.UpdateTime = DateTime.Now;
        }
        if (catalogs.Any())
        {
            await _dbContext.Db.Updateable(catalogs).ExecuteCommandAsync();
        }

        wiki.IsDeleted = true;
        wiki.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(wiki).ExecuteCommandAsync() > 0;
    }

    public async Task<WikiDto> GetByIdAsync(long id)
    {
        var wiki = await _dbContext.WikiDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (wiki == null)
        {
            throw new Exception("知识库不存在");
        }

        return wiki.Adapt<WikiDto>();
    }

    public async Task<PageDto<WikiDto>> GetAdminPageAsync(WikiPageRequest request)
    {
        var query = _dbContext.WikiDb.Where(it => !it.IsDeleted);

        // 按标题模糊查询
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            query = query.Where(it => it.Title.Contains(request.Title));
        }

        // 按日期范围查询
        if (!string.IsNullOrWhiteSpace(request.StartDate))
        {
            var startDate = DateTime.Parse(request.StartDate);
            query = query.Where(it => it.CreateTime >= startDate);
        }
        if (!string.IsNullOrWhiteSpace(request.EndDate))
        {
            var endDate = DateTime.Parse(request.EndDate).AddDays(1);
            query = query.Where(it => it.CreateTime < endDate);
        }

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(it => it.Weight)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PageDto<WikiDto>
        {
            List = list.Adapt<List<WikiDto>>(),
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<List<WikiDto>> GetPublishListAsync()
    {
        var list = await _dbContext.WikiDb
            .Where(it => !it.IsDeleted && it.IsPublish)
            .OrderByDescending(it => it.Weight)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .ToListAsync();

        var dtos = list.Adapt<List<WikiDto>>();

        foreach (var dto in dtos)
        {
            var firstCatalog = await _dbContext.WikiCatalogDb
                .Where(it => it.WikiId == dto.Id && !it.IsDeleted && it.ArticleId != null)
                .OrderBy(it => it.Sort)
                .FirstAsync();
            dto.FirstArticleId = firstCatalog?.ArticleId;
        }

        return dtos;
    }

    public async Task<bool> UpdateIsTopAsync(long id, bool isTop)
    {
        var wiki = await _dbContext.WikiDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (wiki == null)
        {
            return false;
        }

        wiki.Weight = isTop ? 1 : 0;
        wiki.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(wiki).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> UpdateIsPublishAsync(long id, bool isPublish)
    {
        var wiki = await _dbContext.WikiDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (wiki == null)
        {
            return false;
        }

        wiki.IsPublish = isPublish;
        wiki.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(wiki).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 获取知识库文章的上一篇/下一篇目录节点
    /// wikiId：知识库ID；articleId：当前目录节点ID
    /// </summary>
    public async Task<WikiPreNextDto> GetPreNextArticleAsync(long wikiId, long articleId)
    {
        // 获取同一Wiki下的所有目录，按 Sort 排序（扁平列表）
        var catalogs = await _dbContext.WikiCatalogDb
            .Where(it => it.WikiId == wikiId && !it.IsDeleted)
            .OrderBy(it => it.Sort)
            .ToListAsync();

        var currentIndex = catalogs.FindIndex(c => c.Id == articleId);

        if (currentIndex < 0)
        {
            return new WikiPreNextDto();
        }

        WikiCatalogDto? pre = null;
        WikiCatalogDto? next = null;

        if (currentIndex > 0)
        {
            pre = catalogs[currentIndex - 1].Adapt<WikiCatalogDto>();
        }

        if (currentIndex < catalogs.Count - 1)
        {
            next = catalogs[currentIndex + 1].Adapt<WikiCatalogDto>();
        }

        return new WikiPreNextDto
        {
            Pre = pre,
            Next = next
        };
    }

    #endregion

    #region Wiki目录管理

    public async Task<WikiCatalogDto> CreateCatalogAsync(CreateWikiCatalogRequest request)
    {
        var catalog = new WikiCatalog
        {
            WikiId = request.WikiId,
            ArticleId = request.ArticleId,
            ParentId = request.ParentId,
            Title = request.Title,
            Level = request.Level,
            Sort = request.Sort,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(catalog).ExecuteReturnIdentityAsync();
        catalog.Id = id;

        return catalog.Adapt<WikiCatalogDto>();
    }

    public async Task<WikiCatalogDto> UpdateCatalogAsync(UpdateWikiCatalogRequest request)
    {
        var catalog = await _dbContext.WikiCatalogDb
            .Where(it => it.Id == request.Id && !it.IsDeleted)
            .FirstAsync();
        if (catalog == null)
        {
            throw new Exception("目录不存在");
        }

        catalog.Title = request.Title;
        catalog.Sort = request.Sort;
        catalog.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(catalog).ExecuteCommandAsync();

        return catalog.Adapt<WikiCatalogDto>();
    }

    public async Task<bool> DeleteCatalogAsync(long id)
    {
        var catalog = await _dbContext.WikiCatalogDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (catalog == null)
        {
            return false;
        }

        // 逻辑删除
        catalog.IsDeleted = true;
        catalog.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(catalog).ExecuteCommandAsync() > 0;
    }

    public async Task<List<WikiCatalogDto>> GetCatalogsAsync(long wikiId)
    {
        var list = await _dbContext.WikiCatalogDb
            .Where(it => it.WikiId == wikiId && !it.IsDeleted)
            .OrderBy(it => it.Sort)
            .ToListAsync();

        // 构建树形结构
        var dtos = list.Adapt<List<WikiCatalogDto>>();
        return BuildCatalogTree(dtos);
    }

    public async Task<List<WikiCatalogDto>> BatchUpdateCatalogsAsync(BatchUpdateWikiCatalogRequest request)
    {
        var wikiId = request.WikiId;
        var catalogs = request.Catalogs;

        // 获取现有的目录
        var existingCatalogs = await _dbContext.WikiCatalogDb
            .Where(it => it.WikiId == wikiId && !it.IsDeleted)
            .ToListAsync();

        // 收集前端传来的所有有效ID（包括负数的临时ID）
        var receivedIds = catalogs
            .SelectMany(c => new[] { c.Id }.Concat(c.Children?.Select(ch => ch.Id) ?? Enumerable.Empty<long>()))
            .Where(id => id > 0)
            .ToHashSet();

        // 只有当前端传了有效的ID时，才执行删除逻辑
        if (receivedIds.Any())
        {
            // 删除不在前端数据中的目录
            var toDelete = existingCatalogs.Where(c => !receivedIds.Contains(c.Id)).ToList();
            foreach (var cat in toDelete)
            {
                cat.IsDeleted = true;
                cat.UpdateTime = DateTime.Now;
            }
            if (toDelete.Any())
            {
                await _dbContext.Db.Updateable(toDelete).ExecuteCommandAsync();
            }
        }

        // 处理每个一级目录及其子目录
        foreach (var catalogDto in catalogs)
        {
            await SaveCatalogRecursive(wikiId, catalogDto, null);
        }

        // 返回更新后的目录列表
        return await GetCatalogsAsync(wikiId);
    }

    private async Task SaveCatalogRecursive(long wikiId, WikiCatalogDto dto, long? parentId)
    {
        WikiCatalog? catalog;

        if (dto.Id > 0)
        {
            // 更新现有目录
            catalog = await _dbContext.WikiCatalogDb
                .Where(it => it.Id == dto.Id && !it.IsDeleted)
                .FirstAsync();

            if (catalog != null)
            {
                catalog.Title = dto.Title;
                catalog.Sort = dto.Sort;
                catalog.ParentId = parentId;
                catalog.UpdateTime = DateTime.Now;
                await _dbContext.Db.Updateable(catalog).ExecuteCommandAsync();
            }
        }
        else
        {
            // 创建新目录
            catalog = new WikiCatalog
            {
                WikiId = wikiId,
                ArticleId = dto.ArticleId,
                Title = dto.Title,
                Level = dto.Level,
                ParentId = parentId,
                Sort = dto.Sort,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                IsDeleted = false
            };
            dto.Id = await _dbContext.Db.Insertable(catalog).ExecuteReturnIdentityAsync();
        }

        // 递归处理子目录
        if (dto.Children != null && dto.Children.Any())
        {
            foreach (var childDto in dto.Children)
            {
                await SaveCatalogRecursive(wikiId, childDto, dto.Id);
            }
        }
    }

    private List<WikiCatalogDto> BuildCatalogTree(List<WikiCatalogDto> catalogs)
    {
        var dict = catalogs.ToDictionary(c => c.Id);
        var roots = new List<WikiCatalogDto>();

        foreach (var catalog in catalogs)
        {
            if (!catalog.ParentId.HasValue || catalog.ParentId.Value == 0)
            {
                roots.Add(catalog);
            }
            else
            {
                var parentId = catalog.ParentId.Value;
                if (dict.TryGetValue(parentId, out var parent))
                {
                    parent.Children ??= new List<WikiCatalogDto>();
                    parent.Children.Add(catalog);
                }
            }
        }

        return roots;
    }

    #endregion
}

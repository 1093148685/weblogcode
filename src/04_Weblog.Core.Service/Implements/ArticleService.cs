using Mapster;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class ArticleService : IArticleService
{
    private const int PublishedStatus = 1;
    private const int DraftStatus = 0;

    private readonly DbContext _dbContext;

    public ArticleService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleRequest request)
    {
        ValidateArticle(request.Title);

        var now = DateTime.Now;
        var article = new Article
        {
            Title = request.Title.Trim(),
            Summary = request.Summary,
            Cover = request.Cover,
            Weight = request.Weight,
            Type = request.Type,
            Status = NormalizeStatus(request.Status),
            CreateTime = now,
            UpdateTime = now
        };

        _dbContext.Db.Ado.BeginTran();
        try
        {
            var id = await _dbContext.Db.Insertable(article).ExecuteReturnIdentityAsync();
            article.Id = id;

            await SaveCategoryAsync(id, request.CategoryId, now);
            await SaveContentAsync(id, request.Content);
            await SaveTagsAsync(id, GetRequestTagIds(request.TagIds, request.Tags));

            _dbContext.Db.Ado.CommitTran();
            return await GetByIdAsync(id);
        }
        catch
        {
            _dbContext.Db.Ado.RollbackTran();
            throw;
        }
    }

    public async Task<ArticleDto> UpdateAsync(UpdateArticleRequest request)
    {
        ValidateArticle(request.Title);

        var article = await GetEditableArticleAsync(request.Id);
        var now = DateTime.Now;

        article.Title = request.Title.Trim();
        article.Summary = request.Summary;
        article.Cover = request.Cover;

        // Weight 表示置顶权重。编辑文章时前端可能不传该值，所以只有非 0 才覆盖。
        if (request.Weight != 0)
        {
            article.Weight = request.Weight;
        }

        article.Type = request.Type;
        article.Status = NormalizeStatus(request.Status);
        article.UpdateTime = now;

        _dbContext.Db.Ado.BeginTran();
        try
        {
            await _dbContext.Db.Updateable(article).ExecuteCommandAsync();
            await ReplaceCategoryAsync(article.Id, request.CategoryId, now);
            await SaveContentAsync(article.Id, request.Content);
            await ReplaceTagsAsync(article.Id, GetRequestTagIds(request.TagIds, request.Tags));

            _dbContext.Db.Ado.CommitTran();
            return await GetByIdAsync(article.Id);
        }
        catch
        {
            _dbContext.Db.Ado.RollbackTran();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var article = await _dbContext.ArticleDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();

        if (article == null)
        {
            return false;
        }

        article.IsDeleted = true;
        article.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(article).ExecuteCommandAsync() > 0;
    }

    public async Task<ArticleDto> GetByIdAsync(long id)
    {
        var article = await _dbContext.ArticleDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();

        if (article == null)
        {
            throw new Exception("文章不存在");
        }

        var articleContent = await _dbContext.ArticleContentDb
            .FirstAsync(it => it.ArticleId == id);

        var categoryMap = await GetCategoryMapAsync(new[] { id });
        var tagMap = await GetTagMapAsync(new[] { id });

        var dto = article.Adapt<ArticleDto>();
        ApplyCategory(dto, categoryMap.GetValueOrDefault(id));
        ApplyTags(dto, tagMap.GetValueOrDefault(id) ?? new List<TagSelectDto>());
        dto.Content = articleContent?.Content;
        return dto;
    }

    public async Task<PageDto<ArticleAdminDto>> GetAdminPageAsync(PageRequest request)
    {
        NormalizePageRequest(request);

        var total = await _dbContext.ArticleDb
            .Where(it => !it.IsDeleted)
            .CountAsync();

        var list = await _dbContext.ArticleDb
            .Where(it => !it.IsDeleted)
            .OrderByDescending(it => it.Weight)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var articleIds = list.Select(it => it.Id).ToList();
        var categoryMap = await GetCategoryMapAsync(articleIds);
        var tagMap = await GetTagMapAsync(articleIds);

        var result = list.Select(article =>
        {
            var dto = article.Adapt<ArticleAdminDto>();
            var category = categoryMap.GetValueOrDefault(article.Id);
            dto.CategoryId = category?.Id ?? 0;
            dto.CategoryName = category?.Name;
            dto.Tags = tagMap.GetValueOrDefault(article.Id) ?? new List<TagSelectDto>();
            return dto;
        }).ToList();

        return new PageDto<ArticleAdminDto>
        {
            List = result,
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<List<TagSelectDto>> GetAllTagsAsync()
    {
        var list = await _dbContext.TagDb
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.Name)
            .ToListAsync();

        return list.Adapt<List<TagSelectDto>>();
    }

    public async Task<bool> UpdateIsTopAsync(long id, bool isTop)
    {
        var article = await _dbContext.ArticleDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();

        if (article == null)
        {
            return false;
        }

        article.Weight = isTop ? 1 : 0;
        article.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(article).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> UpdateStatusAsync(long id, int status)
    {
        var article = await _dbContext.ArticleDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();

        if (article == null)
        {
            return false;
        }

        article.Status = NormalizeStatus(status);
        article.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(article).ExecuteCommandAsync() > 0;
    }

    private async Task<Article> GetEditableArticleAsync(long id)
    {
        var article = await _dbContext.ArticleDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();

        return article ?? throw new Exception("文章不存在");
    }

    private async Task SaveCategoryAsync(long articleId, long categoryId, DateTime now)
    {
        if (categoryId <= 0) return;

        await _dbContext.Db.Insertable(new ArticleCategoryRel
        {
            ArticleId = articleId,
            CategoryId = categoryId,
            CreateTime = now
        }).ExecuteCommandAsync();
    }

    private async Task ReplaceCategoryAsync(long articleId, long categoryId, DateTime now)
    {
        await _dbContext.Db.Deleteable<ArticleCategoryRel>()
            .Where(it => it.ArticleId == articleId)
            .ExecuteCommandAsync();

        await SaveCategoryAsync(articleId, categoryId, now);
    }

    private async Task SaveContentAsync(long articleId, string? content)
    {
        if (content == null) return;

        var existingContent = await _dbContext.ArticleContentDb
            .FirstAsync(it => it.ArticleId == articleId);

        if (existingContent == null)
        {
            await _dbContext.Db.Insertable(new ArticleContent
            {
                ArticleId = articleId,
                Content = content
            }).ExecuteCommandAsync();
            return;
        }

        existingContent.Content = content;
        await _dbContext.Db.Updateable(existingContent).ExecuteCommandAsync();
    }

    private async Task SaveTagsAsync(long articleId, List<long> tagIds)
    {
        if (tagIds.Count == 0) return;

        var articleTags = tagIds.Select(tagId => new ArticleTag
        {
            ArticleId = articleId,
            TagId = tagId
        }).ToList();

        await _dbContext.Db.Insertable(articleTags).ExecuteCommandAsync();
    }

    private async Task ReplaceTagsAsync(long articleId, List<long> tagIds)
    {
        await _dbContext.Db.Deleteable<ArticleTag>()
            .Where(it => it.ArticleId == articleId)
            .ExecuteCommandAsync();

        await SaveTagsAsync(articleId, tagIds);
    }

    private async Task<Dictionary<long, CategorySimpleDto>> GetCategoryMapAsync(IEnumerable<long> articleIds)
    {
        var ids = articleIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, CategorySimpleDto>();

        var relations = await _dbContext.ArticleCategoryRelDb
            .Where(it => ids.Contains(it.ArticleId))
            .ToListAsync();

        var categoryIds = relations.Select(it => it.CategoryId).Distinct().ToList();
        if (categoryIds.Count == 0) return new Dictionary<long, CategorySimpleDto>();

        var categories = await _dbContext.CategoryDb
            .Where(it => categoryIds.Contains(it.Id))
            .ToListAsync();

        var categoriesById = categories.ToDictionary(
            it => it.Id,
            it => new CategorySimpleDto { Id = it.Id, Name = it.Name });

        return relations
            .Where(it => categoriesById.ContainsKey(it.CategoryId))
            .GroupBy(it => it.ArticleId)
            .ToDictionary(it => it.Key, it => categoriesById[it.First().CategoryId]);
    }

    private async Task<Dictionary<long, List<TagSelectDto>>> GetTagMapAsync(IEnumerable<long> articleIds)
    {
        var ids = articleIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, List<TagSelectDto>>();

        var relations = await _dbContext.ArticleTagDb
            .Where(it => ids.Contains(it.ArticleId))
            .ToListAsync();

        var tagIds = relations.Select(it => it.TagId).Distinct().ToList();
        if (tagIds.Count == 0) return ids.ToDictionary(id => id, _ => new List<TagSelectDto>());

        var tags = await _dbContext.TagDb
            .Where(it => tagIds.Contains(it.Id) && !it.IsDeleted)
            .ToListAsync();

        var tagsById = tags.ToDictionary(it => it.Id, it => new TagSelectDto { Id = it.Id, Name = it.Name });

        return relations
            .Where(it => tagsById.ContainsKey(it.TagId))
            .GroupBy(it => it.ArticleId)
            .ToDictionary(
                it => it.Key,
                it => it.Select(rel => tagsById[rel.TagId]).ToList());
    }

    private static void ApplyCategory(ArticleDto dto, CategorySimpleDto? category)
    {
        dto.CategoryId = category?.Id ?? 0;
        dto.CategoryName = category?.Name;
        dto.Category = category;
    }

    private static void ApplyTags(ArticleDto dto, List<TagSelectDto> tags)
    {
        dto.Tags = tags;
        dto.TagIds = tags.Select(it => it.Id).ToList();
    }

    private static List<long> GetRequestTagIds(List<long>? tagIds, List<long>? tags)
    {
        return (tagIds ?? tags ?? new List<long>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static int NormalizeStatus(int status)
    {
        return status == PublishedStatus ? PublishedStatus : DraftStatus;
    }

    private static void NormalizePageRequest(PageRequest request)
    {
        request.PageNum = request.PageNum <= 0 ? 1 : request.PageNum;
        request.PageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
    }

    private static void ValidateArticle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("文章标题不能为空");
        }
    }
}

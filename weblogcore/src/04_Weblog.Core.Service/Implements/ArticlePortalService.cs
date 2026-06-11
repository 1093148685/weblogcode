using Mapster;
using SqlSugar;
using Markdig;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class ArticlePortalService : IArticlePortalService
{
    private readonly DbContext _dbContext;

    public ArticlePortalService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 批量构建 ArticleDto（一次查询所有关联数据，避免 N+1）
    /// </summary>
    private async Task<List<ArticleDto>> BuildArticleDtosAsync(List<Article> articles)
    {
        if (articles.Count == 0) return new List<ArticleDto>();

        var articleIds = articles.Select(a => a.Id).ToList();

        // 1. 批量查分类关联
        var allCategoryRels = await _dbContext.ArticleCategoryRelDb
            .Where(it => articleIds.Contains(it.ArticleId))
            .ToListAsync();
        var catRelDict = allCategoryRels.ToDictionary(r => r.ArticleId);

        // 2. 批量查分类名称
        var categoryIds = allCategoryRels.Select(r => r.CategoryId).Distinct().ToList();
        var categories = categoryIds.Count > 0
            ? await _dbContext.CategoryDb.Where(it => categoryIds.Contains(it.Id)).ToListAsync()
            : new List<Category>();
        var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

        // 3. 批量查标签关联
        var allArticleTags = await _dbContext.ArticleTagDb
            .Where(it => articleIds.Contains(it.ArticleId))
            .ToListAsync();

        // 4. 批量查标签名称
        var tagIds = allArticleTags.Select(t => t.TagId).Distinct().ToList();
        var tags = tagIds.Count > 0
            ? await _dbContext.TagDb.Where(it => tagIds.Contains(it.Id)).ToListAsync()
            : new List<Tag>();
        var tagDict = tags.ToDictionary(t => t.Id, t => t.Name);

        // 5. 按文章分组标签
        var tagsByArticle = allArticleTags
            .GroupBy(t => t.ArticleId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new TagSelectDto { Id = t.TagId, Name = tagDict.GetValueOrDefault(t.TagId, "") }).ToList()
            );

        // 6. 组装 DTO
        var result = new List<ArticleDto>();
        foreach (var article in articles)
        {
            var dto = article.Adapt<ArticleDto>();
            dto.Content = null;

            if (catRelDict.TryGetValue(article.Id, out var catRel))
            {
                dto.CategoryId = catRel.CategoryId;
                dto.CategoryName = categoryDict.GetValueOrDefault(catRel.CategoryId);
                dto.Category = new CategorySimpleDto
                {
                    Id = catRel.CategoryId,
                    Name = categoryDict.GetValueOrDefault(catRel.CategoryId, "")
                };
            }

            dto.Tags = tagsByArticle.GetValueOrDefault(article.Id, new List<TagSelectDto>());
            result.Add(dto);
        }

        return result;
    }

    public async Task<PageDto<ArticleDto>> GetPageAsync(PageRequest request)
    {
        var baseQuery = _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted);

        var total = await baseQuery.CountAsync();

        var list = await baseQuery
            .OrderByDescending(it => it.Weight)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(it => new Article
            {
                Id = it.Id,
                Title = it.Title,
                Cover = it.Cover,
                Summary = it.Summary,
                CreateTime = it.CreateTime,
                ReadNum = it.ReadNum,
                Weight = it.Weight,
                Status = it.Status
            })
            .ToListAsync();

        return new PageDto<ArticleDto>
        {
            List = await BuildArticleDtosAsync(list),
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<PageDto<ArticleDto>> GetArchivePageAsync(PageRequest request)
    {
        return await GetPageAsync(request);
    }

    public async Task<List<ArchiveArticleDto>> GetArchiveListAsync(int? size = null)
    {
        var query = _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Desc);

        var limit = size ?? 50;
        if (limit > 0)
        {
            query = query.Take(limit);
        }

        var articles = await query.Select(it => new Article
        {
            Id = it.Id,
            Title = it.Title,
            Cover = it.Cover,
            Summary = it.Summary,
            CreateTime = it.CreateTime,
            Weight = it.Weight,
            Status = it.Status
        }).ToListAsync();

        if (articles.Count == 0) return new List<ArchiveArticleDto>();

        var articleDtos = await BuildArticleDtosAsync(articles);
        var dtoDict = articleDtos.ToDictionary(d => d.Id);

        return articles
            .GroupBy(a => new { a.CreateTime.Year, a.CreateTime.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new ArchiveArticleDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Articles = g.Select(a => dtoDict.GetValueOrDefault(a.Id)).Where(d => d != null).ToList()!
            })
            .ToList();
    }

    public async Task<PageDto<ArticleDto>> GetPageByCategoryAsync(long categoryId, PageRequest request)
    {
        var articleIds = await _dbContext.ArticleCategoryRelDb
            .Where(it => it.CategoryId == categoryId)
            .Select(it => it.ArticleId)
            .ToListAsync();

        var total = await _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted && articleIds.Contains(it.Id))
            .CountAsync();

        var list = await _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted && articleIds.Contains(it.Id))
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(it => new Article
            {
                Id = it.Id,
                Title = it.Title,
                Cover = it.Cover,
                Summary = it.Summary,
                CreateTime = it.CreateTime,
                ReadNum = it.ReadNum,
                Weight = it.Weight,
                Status = it.Status
            })
            .ToListAsync();

        return new PageDto<ArticleDto>
        {
            List = await BuildArticleDtosAsync(list),
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<PageDto<ArticleDto>> GetPageByTagAsync(long tagId, PageRequest request)
    {
        var articleIds = await _dbContext.ArticleTagDb
            .Where(it => it.TagId == tagId)
            .Select(it => it.ArticleId)
            .ToListAsync();

        var total = await _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted && articleIds.Contains(it.Id))
            .CountAsync();

        var list = await _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted && articleIds.Contains(it.Id))
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(it => new Article
            {
                Id = it.Id,
                Title = it.Title,
                Cover = it.Cover,
                Summary = it.Summary,
                CreateTime = it.CreateTime,
                ReadNum = it.ReadNum,
                Weight = it.Weight,
                Status = it.Status
            })
            .ToListAsync();

        return new PageDto<ArticleDto>
        {
            List = await BuildArticleDtosAsync(list),
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<ArticleDto> GetByIdAsync(long id)
    {
        var article = await _dbContext.ArticleDb
            .Where(it => it.Id == id && it.Status == 1 && !it.IsDeleted)
            .FirstAsync();

        if (article == null)
        {
            throw new Exception("文章不存在");
        }

        // 获取分类
        var categoryRel = await _dbContext.ArticleCategoryRelDb
            .FirstAsync(it => it.ArticleId == id);

        string? categoryName = null;
        long categoryId = 0;
        if (categoryRel != null)
        {
            categoryId = categoryRel.CategoryId;
            var category = await _dbContext.CategoryDb
                .FirstAsync(it => it.Id == categoryRel.CategoryId);
            categoryName = category?.Name;
        }

        // 获取标签
        var articleTags = await _dbContext.ArticleTagDb
            .Where(it => it.ArticleId == id)
            .ToListAsync();

        var tagIds = articleTags.Select(t => t.TagId).ToList();
        var tags = tagIds.Count > 0
            ? await _dbContext.TagDb.Where(it => tagIds.Contains(it.Id)).ToListAsync()
            : new List<Tag>();
        var tagDict = tags.ToDictionary(t => t.Id, t => t.Name);

        var tagDtos = articleTags
            .Select(at => new TagSelectDto { Id = at.TagId, Name = tagDict.GetValueOrDefault(at.TagId, "") })
            .ToList();

        // 获取文章内容
        var articleContent = await _dbContext.ArticleContentDb
            .FirstAsync(it => it.ArticleId == id);

        var markdown = articleContent?.Content ?? "";
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        var htmlContent = Markdown.ToHtml(markdown, pipeline);

        var dto = article.Adapt<ArticleDto>();
        dto.CategoryId = categoryId;
        dto.CategoryName = categoryName;
        dto.Tags = tagDtos;
        dto.Content = htmlContent;
        return dto;
    }

    public async Task IncrementViewCountAsync(long id)
    {
        await _dbContext.Db.Updateable<Article>()
            .SetColumns(it => it.ReadNum == it.ReadNum + 1)
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task<PageDto<ArticleDto>> GetPageByKeywordAsync(string keyword, PageRequest request)
    {
        var query = _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(it => it.Title.Contains(keyword) || it.Summary.Contains(keyword));
        }

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(it => new Article
            {
                Id = it.Id,
                Title = it.Title,
                Cover = it.Cover,
                Summary = it.Summary,
                CreateTime = it.CreateTime,
                ReadNum = it.ReadNum,
                Weight = it.Weight,
                Status = it.Status
            })
            .ToListAsync();

        return new PageDto<ArticleDto>
        {
            List = await BuildArticleDtosAsync(list),
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<PreNextArticleDto> GetPreNextArticleAsync(long articleId)
    {
        var currentArticle = await _dbContext.ArticleDb
            .Where(it => it.Id == articleId && it.Status == 1 && !it.IsDeleted)
            .FirstAsync();

        if (currentArticle == null)
        {
            return new PreNextArticleDto();
        }

        var preArticle = await _dbContext.ArticleDb
            .Where(it => it.CreateTime < currentArticle.CreateTime
                         && it.Status == 1 && !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .FirstAsync();

        var nextArticle = await _dbContext.ArticleDb
            .Where(it => it.CreateTime > currentArticle.CreateTime
                         && it.Status == 1 && !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Asc)
            .FirstAsync();

        return new PreNextArticleDto
        {
            PreArticle = preArticle != null
                ? new PreNextArticleItemDto { Id = preArticle.Id, Title = preArticle.Title }
                : null,
            NextArticle = nextArticle != null
                ? new PreNextArticleItemDto { Id = nextArticle.Id, Title = nextArticle.Title }
                : null
        };
    }
}

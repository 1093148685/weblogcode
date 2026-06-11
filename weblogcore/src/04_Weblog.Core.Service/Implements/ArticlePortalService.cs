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
    /// 从文章列表构建 ArticleDto（获取分类名和标签）
    /// </summary>
    private async Task<ArticleDto> BuildArticleDtoAsync(Article article)
    {
        // 通过关联表获取分类 ID
        var categoryRel = await _dbContext.ArticleCategoryRelDb
            .FirstAsync(it => it.ArticleId == article.Id);

        string? categoryName = null;
        long categoryId = 0;
        if (categoryRel != null)
        {
            categoryId = categoryRel.CategoryId;
            var category = await _dbContext.CategoryDb
                .FirstAsync(it => it.Id == categoryId);
            categoryName = category?.Name;
        }

        // 获取标签列表
        var articleTags = await _dbContext.ArticleTagDb
            .Where(it => it.ArticleId == article.Id)
            .ToListAsync();

        var tags = new List<TagSelectDto>();
        foreach (var at in articleTags)
        {
            var tag = await _dbContext.TagDb.FirstAsync(it => it.Id == at.TagId);
            if (tag != null)
            {
                tags.Add(new TagSelectDto { Id = tag.Id, Name = tag.Name });
            }
        }

        var dto = article.Adapt<ArticleDto>();
        dto.CategoryId = categoryId;
        dto.CategoryName = categoryName;
        dto.Category = categoryId > 0 ? new CategorySimpleDto { Id = categoryId, Name = categoryName ?? "" } : null;
        dto.Tags = tags;
        dto.Content = null;
        return dto;
    }

    public async Task<PageDto<ArticleDto>> GetPageAsync(PageRequest request)
    {
        var total = await _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted)
            .CountAsync();

        var list = await _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted)
            .OrderByDescending(it => it.Weight)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var result = new List<ArticleDto>();
        foreach (var article in list)
        {
            result.Add(await BuildArticleDtoAsync(article));
        }

        return new PageDto<ArticleDto>
        {
            List = result,
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<PageDto<ArticleDto>> GetArchivePageAsync(PageRequest request)
    {
        return await GetPageAsync(request);
    }

    public async Task<List<ArchiveArticleDto>> GetArchiveListAsync()
    {
        var articles = await _dbContext.ArticleDb
            .Where(it => it.Status == 1 && !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .ToListAsync();

        var result = new List<ArchiveArticleDto>();

        var groupedArticles = articles
            .GroupBy(a => new { a.CreateTime.Year, a.CreateTime.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month);

        foreach (var group in groupedArticles)
        {
            var yearMonth = $"{group.Key.Year}-{group.Key.Month:D2}";
            var articleDtos = new List<ArticleDto>();

            foreach (var article in group)
            {
                var dto = await BuildArticleDtoAsync(article);
                articleDtos.Add(dto);
            }

            result.Add(new ArchiveArticleDto
            {
                Month = yearMonth,
                Articles = articleDtos
            });
        }

        return result;
    }

    public async Task<PageDto<ArticleDto>> GetPageByCategoryAsync(long categoryId, PageRequest request)
    {
        // 通过关联表查询该分类下的文章 ID
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
            .ToListAsync();

        var result = new List<ArticleDto>();
        foreach (var article in list)
        {
            result.Add(await BuildArticleDtoAsync(article));
        }

        return new PageDto<ArticleDto>
        {
            List = result,
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
            .ToListAsync();

        var result = new List<ArticleDto>();
        foreach (var article in list)
        {
            result.Add(await BuildArticleDtoAsync(article));
        }

        return new PageDto<ArticleDto>
        {
            List = result,
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

        // 通过关联表获取分类
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

        var articleTags = await _dbContext.ArticleTagDb
            .Where(it => it.ArticleId == id)
            .ToListAsync();

        var tags = new List<TagSelectDto>();
        foreach (var at in articleTags)
        {
            var tag = await _dbContext.TagDb.FirstAsync(it => it.Id == at.TagId);
            if (tag != null)
            {
                tags.Add(new TagSelectDto { Id = tag.Id, Name = tag.Name });
            }
        }

        // 获取文章内容
        var articleContent = await _dbContext.ArticleContentDb
            .FirstAsync(it => it.ArticleId == id);

        // 将 Markdown 转换为 HTML
        var markdown = articleContent?.Content ?? "";
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        var htmlContent = Markdown.ToHtml(markdown, pipeline);

        var dto = article.Adapt<ArticleDto>();
        dto.CategoryId = categoryId;
        dto.CategoryName = categoryName;
        dto.Tags = tags;
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
            .ToListAsync();

        var result = new List<ArticleDto>();
        foreach (var article in list)
        {
            result.Add(await BuildArticleDtoAsync(article));
        }

        return new PageDto<ArticleDto>
        {
            List = result,
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

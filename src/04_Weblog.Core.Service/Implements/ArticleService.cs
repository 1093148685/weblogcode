using Mapster;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class ArticleService : IArticleService
{
    private readonly DbContext _dbContext;

    public ArticleService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleRequest request)
    {
        var article = new Article
        {
            Title = request.Title,
            Summary = request.Summary,
            Cover = request.Cover,
            Weight = request.Weight,
            Type = request.Type,
            Status = request.Status,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(article).ExecuteReturnIdentityAsync();
        article.Id = id;

        // 保存文章与分类的关联
        if (request.CategoryId > 0)
        {
            var categoryRel = new ArticleCategoryRel
            {
                ArticleId = id,
                CategoryId = request.CategoryId,
                CreateTime = DateTime.Now
            };
            await _dbContext.Db.Insertable(categoryRel).ExecuteCommandAsync();
        }

        // 保存文章内容到独立表
        if (!string.IsNullOrEmpty(request.Content))
        {
            var articleContent = new ArticleContent
            {
                ArticleId = id,
                Content = request.Content
            };
            await _dbContext.Db.Insertable(articleContent).ExecuteCommandAsync();
        }

        // 保存文章标签关联
        var tagIds = request.TagIds ?? request.Tags;
        if (tagIds != null && tagIds.Count > 0)
        {
            var articleTags = tagIds.Select(tagId => new ArticleTag
            {
                ArticleId = id,
                TagId = tagId
            }).ToList();
            await _dbContext.Db.Insertable(articleTags).ExecuteCommandAsync();
        }

        return await GetByIdAsync(article.Id);
    }

    public async Task<ArticleDto> UpdateAsync(UpdateArticleRequest request)
    {
        var article = await _dbContext.ArticleDb
            .Where(it => it.Id == request.Id && !it.IsDeleted)
            .FirstAsync();
        if (article == null)
        {
            throw new Exception("文章不存在");
        }

        article.Title = request.Title;
        article.Summary = request.Summary;
        article.Cover = request.Cover;
        // 只有当传入的 Weight 不为 0 时才更新置顶状态，避免编辑时误取消置顶
        if (request.Weight != 0)
        {
            article.Weight = request.Weight;
        }
        article.Type = request.Type;
        article.Status = request.Status;
        article.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(article).ExecuteCommandAsync();

        // 更新文章与分类的关联
        await _dbContext.Db.Deleteable<ArticleCategoryRel>()
            .Where(it => it.ArticleId == article.Id)
            .ExecuteCommandAsync();
        if (request.CategoryId > 0)
        {
            var categoryRel = new ArticleCategoryRel
            {
                ArticleId = article.Id,
                CategoryId = request.CategoryId,
                CreateTime = DateTime.Now
            };
            await _dbContext.Db.Insertable(categoryRel).ExecuteCommandAsync();
        }

        // 更新文章内容
        if (!string.IsNullOrEmpty(request.Content))
        {
            var existingContent = await _dbContext.ArticleContentDb
                .FirstAsync(it => it.ArticleId == article.Id);
            if (existingContent != null)
            {
                existingContent.Content = request.Content;
                await _dbContext.Db.Updateable(existingContent).ExecuteCommandAsync();
            }
            else
            {
                var articleContent = new ArticleContent
                {
                    ArticleId = article.Id,
                    Content = request.Content
                };
                await _dbContext.Db.Insertable(articleContent).ExecuteCommandAsync();
            }
        }

        // 更新文章标签关联
        await _dbContext.Db.Deleteable<ArticleTag>()
            .Where(it => it.ArticleId == article.Id)
            .ExecuteCommandAsync();
        var updateTagIds = request.TagIds ?? request.Tags;
        if (updateTagIds != null && updateTagIds.Count > 0)
        {
            var articleTags = updateTagIds.Select(tagId => new ArticleTag
            {
                ArticleId = article.Id,
                TagId = tagId
            }).ToList();
            await _dbContext.Db.Insertable(articleTags).ExecuteCommandAsync();
        }

        return await GetByIdAsync(article.Id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        // 软删除：仅标记 IsDeleted = true
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

        // 获取文章内容
        var articleContent = await _dbContext.ArticleContentDb
            .FirstAsync(it => it.ArticleId == id);

        // 通过关联表获取分类
        var categoryRel = await _dbContext.ArticleCategoryRelDb
            .FirstAsync(it => it.ArticleId == id);
        long categoryId = categoryRel?.CategoryId ?? 0;

        string? categoryName = null;
        if (categoryId > 0)
        {
            var category = await _dbContext.CategoryDb
                .FirstAsync(it => it.Id == categoryId);
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

        var dto = article.Adapt<ArticleDto>();
        dto.CategoryId = categoryId;
        dto.CategoryName = categoryName;
        dto.Content = articleContent?.Content;
        dto.Tags = tags;
        dto.TagIds = articleTags.Select(at => at.TagId).ToList();
        return dto;
    }

    public async Task<PageDto<ArticleAdminDto>> GetAdminPageAsync(PageRequest request)
    {
        // 过滤已删除的文章
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

        var result = new List<ArticleAdminDto>();
        foreach (var article in list)
        {
            // 通过关联表获取分类
            var categoryRel = await _dbContext.ArticleCategoryRelDb
                .FirstAsync(it => it.ArticleId == article.Id);
            long categoryId = categoryRel?.CategoryId ?? 0;

            string? categoryName = null;
            if (categoryId > 0)
            {
                var category = await _dbContext.CategoryDb
                    .FirstAsync(it => it.Id == categoryId);
                categoryName = category?.Name;
            }

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

            var dto = article.Adapt<ArticleAdminDto>();
            dto.CategoryId = categoryId;
            dto.CategoryName = categoryName;
            dto.Tags = tags;
            result.Add(dto);
        }

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
}

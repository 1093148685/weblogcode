using Mapster;
using SqlSugar;
using System.Linq;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class TagService : ITagService
{
    private readonly DbContext _dbContext;

    public TagService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TagDto> CreateAsync(CreateTagRequest request)
    {
        // 如果有批量添加的标签列表
        if (request.Tags != null && request.Tags.Count > 0)
        {
            var tags = request.Tags.Select(name => new Tag
            {
                Name = name,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ToList();

            await _dbContext.Db.Insertable(tags).ExecuteCommandAsync();
            
            // 返回第一个创建的标签
            var firstTag = tags.First();
            var savedTag = await _dbContext.TagDb.FirstAsync(it => it.Name == firstTag.Name);
            return savedTag.Adapt<TagDto>();
        }

        // 单个标签创建
        var tag = new Tag
        {
            Name = request.Name,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(tag).ExecuteReturnIdentityAsync();
        tag.Id = id;
        return tag.Adapt<TagDto>();
    }

    public async Task<TagDto> UpdateAsync(UpdateTagRequest request)
    {
        var tag = await _dbContext.TagDb.FirstAsync(it => it.Id == request.Id);
        if (tag == null)
        {
            throw new Exception("标签不存在");
        }

        tag.Name = request.Name;
        tag.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(tag).ExecuteCommandAsync();
        return tag.Adapt<TagDto>();
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var tag = await _dbContext.TagDb.FirstAsync(it => it.Id == id);
        if (tag == null)
        {
            return false;
        }

        tag.IsDeleted = true;
        tag.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(tag).ExecuteCommandAsync() > 0;
    }

    public async Task<PageDto<TagDto>> GetPageAsync(TagPageRequest request)
    {
        var query = _dbContext.TagDb.Where(it => !it.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(it => it.Name.Contains(request.Keyword));
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
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // 获取每个标签的文章数量
        var dtos = list.Adapt<List<TagDto>>();
        foreach (var dto in dtos)
        {
            dto.ArticlesTotal = await _dbContext.ArticleTagDb
                .Where(it => it.TagId == dto.Id)
                .CountAsync();
        }

        return new PageDto<TagDto>
        {
            List = dtos,
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<List<TagSelectDto>> GetSelectListAsync()
    {
        var list = await _dbContext.TagDb
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.CreateTime)
            .ToListAsync();
        return list.Adapt<List<TagSelectDto>>();
    }

    public async Task<List<TagSelectDto>> SearchSelectListAsync(string keyword)
    {
        var list = await _dbContext.TagDb
            .Where(it => !it.IsDeleted && it.Name.Contains(keyword))
            .OrderBy(it => it.Name)
            .Take(10)
            .ToListAsync();
        return list.Adapt<List<TagSelectDto>>();
    }

    public async Task<bool> HasArticleAsync(long id)
    {
        var count = await _dbContext.ArticleTagDb.Where(it => it.TagId == id).CountAsync();
        return count > 0;
    }

    public async Task<List<TagDto>> GetListAsync(int? size = null)
    {
        var query = _dbContext.TagDb
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Desc);

        if (size.HasValue && size.Value > 0)
        {
            query = query.Take(size.Value);
        }

        var list = await query.ToListAsync();

        var result = new List<TagDto>();
        foreach (var tag in list)
        {
            var count = await _dbContext.ArticleTagDb.Where(it => it.TagId == tag.Id).CountAsync();
            var dto = tag.Adapt<TagDto>();
            dto.ArticlesTotal = count;
            result.Add(dto);
        }

        return result;
    }
}

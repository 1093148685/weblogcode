using Mapster;
using SqlSugar;
using System.Linq;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class CategoryService : ICategoryService
{
    private readonly DbContext _dbContext;

    public CategoryService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(category).ExecuteReturnIdentityAsync();
        category.Id = id;
        return category.Adapt<CategoryDto>();
    }

    public async Task<CategoryDto> UpdateAsync(UpdateCategoryRequest request)
    {
        var category = await _dbContext.CategoryDb.FirstAsync(it => it.Id == request.Id);
        if (category == null)
        {
            throw new Exception("分类不存在");
        }

        category.Name = request.Name;
        category.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(category).ExecuteCommandAsync();
        return category.Adapt<CategoryDto>();
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var category = await _dbContext.CategoryDb.FirstAsync(it => it.Id == id);
        if (category == null)
        {
            return false;
        }

        category.IsDeleted = true;
        category.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(category).ExecuteCommandAsync() > 0;
    }

    public async Task<PageDto<CategoryDto>> GetPageAsync(CategoryPageRequest request)
    {
        var query = _dbContext.CategoryDb.Where(it => !it.IsDeleted);

        // 按名称模糊查询
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(it => it.Name.Contains(request.Name));
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

        // 获取每个分类的文章数量
        var dtos = list.Adapt<List<CategoryDto>>();
        foreach (var dto in dtos)
        {
            dto.ArticlesTotal = await _dbContext.ArticleCategoryRelDb
                .Where(it => it.CategoryId == dto.Id)
                .CountAsync();
        }

        return new PageDto<CategoryDto>
        {
            List = dtos,
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<List<CategorySelectDto>> GetSelectListAsync()
    {
        var list = await _dbContext.CategoryDb
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.CreateTime)
            .ToListAsync();
        return list.Adapt<List<CategorySelectDto>>();
    }

    public async Task<bool> HasArticleAsync(long id)
    {
        var count = await _dbContext.ArticleCategoryRelDb.Where(it => it.CategoryId == id).CountAsync();
        return count > 0;
    }

    public async Task<List<CategoryDto>> GetListAsync(int? size = null)
    {
        var query = _dbContext.CategoryDb
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Desc);

        if (size.HasValue && size.Value > 0)
        {
            query = query.Take(size.Value);
        }

        var list = await query.ToListAsync();

        var result = new List<CategoryDto>();
        foreach (var category in list)
        {
            var count = await _dbContext.ArticleCategoryRelDb.Where(it => it.CategoryId == category.Id).CountAsync();
            var dto = category.Adapt<CategoryDto>();
            dto.ArticlesTotal = count;
            result.Add(dto);
        }

        return result;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/category")]
[ApiController]
[OutputCache]
public class CategoryPortalController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IArticlePortalService _articlePortalService;

    public CategoryPortalController(ICategoryService categoryService, IArticlePortalService articlePortalService)
    {
        _categoryService = categoryService;
        _articlePortalService = articlePortalService;
    }

    [HttpPost("list")]
    public async Task<Result<List<CategoryDto>>> GetList([FromBody] CategoryListRequest? request)
    {
        var result = await _categoryService.GetListAsync(request?.Size);
        return Result<List<CategoryDto>>.Ok(result);
    }

    [HttpPost("select/list")]
    public async Task<Result<List<CategorySelectDto>>> GetSelectList([FromBody] PageRequest? request)
    {
        var result = await _categoryService.GetSelectListAsync();
        return Result<List<CategorySelectDto>>.Ok(result);
    }

    [HttpPost("article/list")]
    public async Task<Result<PageDto<ArticleDto>>> GetCategoryArticleList([FromBody] CategoryArticleRequest request)
    {
        var categoryId = request.CategoryId > 0 ? request.CategoryId : request.Id;
        var pageNum = request.PageNum > 0 ? request.PageNum : (request.Current > 0 ? request.Current : 1);
        var pageSize = request.PageSize > 0 ? request.PageSize : (request.Size > 0 ? request.Size : 10);
        
        var pageRequest = new PageRequest
        {
            PageNum = pageNum,
            PageSize = pageSize
        };
        var result = await _articlePortalService.GetPageByCategoryAsync(categoryId, pageRequest);
        return Result<PageDto<ArticleDto>>.Ok(result);
    }
}

public class CategoryListRequest
{
    public int? Size { get; set; }
}

public class CategoryArticleRequest
{
    public long CategoryId { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public long Id { get; set; } // 兼容前端
    public int Current { get; set; } // 兼容前端
    public int Size { get; set; } // 兼容前端
}

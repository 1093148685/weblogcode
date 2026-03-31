using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/category")]
[ApiController]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<CategoryDto>>> GetPage([FromBody] CategoryPageRequest request)
    {
        var result = await _categoryService.GetPageAsync(request);
        return Result<PageDto<CategoryDto>>.Ok(result);
    }

    [HttpPost("add")]
    [RequireRole("admin")]
    public async Task<Result<CategoryDto>> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _categoryService.CreateAsync(request);
        return Result<CategoryDto>.Ok(result);
    }

    [HttpPost("delete")]
    [RequireRole("admin")]
    public async Task<Result> Delete([FromBody] IdRequest request)
    {
        var hasArticle = await _categoryService.HasArticleAsync(request.Id);
        if (hasArticle)
        {
            return Result.Fail("该分类下存在文章，无法删除");
        }
        var result = await _categoryService.DeleteAsync(request.Id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }

    [HttpPost("select/list")]
    public async Task<Result<List<CategorySelectDto>>> GetSelectList()
    {
        var result = await _categoryService.GetSelectListAsync();
        return Result<List<CategorySelectDto>>.Ok(result);
    }
}

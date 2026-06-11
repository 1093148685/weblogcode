using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/tag")]
[ApiController]
[Authorize]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpPost("list")]
    public async Task<Result<PageDto<TagDto>>> GetPage([FromBody] TagPageRequest request)
    {
        var result = await _tagService.GetPageAsync(request);
        return Result<PageDto<TagDto>>.Ok(result);
    }

    [HttpPost("add")]
    [RequireRole("admin")]
    public async Task<Result<TagDto>> Create([FromBody] CreateTagRequest request)
    {
        var result = await _tagService.CreateAsync(request);
        return Result<TagDto>.Ok(result);
    }

    [HttpPost("delete")]
    [RequireRole("admin")]
    public async Task<Result> Delete([FromBody] IdRequest request)
    {
        var hasArticle = await _tagService.HasArticleAsync(request.Id);
        if (hasArticle)
        {
            return Result.Fail("该标签下存在文章，无法删除");
        }
        var result = await _tagService.DeleteAsync(request.Id);
        return result ? Result.Ok() : Result.Fail("删除失败");
    }

    [HttpPost("select/list")]
    public async Task<Result<List<TagSelectDto>>> GetSelectList()
    {
        var result = await _tagService.GetSelectListAsync();
        return Result<List<TagSelectDto>>.Ok(result);
    }

    [HttpPost("search")]
    public async Task<Result<List<TagSelectDto>>> Search([FromBody] SearchRequest request)
    {
        var result = await _tagService.SearchSelectListAsync(request.Key);
        return Result<List<TagSelectDto>>.Ok(result);
    }
}

public class SearchRequest
{
    public string Key { get; set; } = string.Empty;
}

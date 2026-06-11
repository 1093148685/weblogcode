using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/tag")]
[ApiController]
[OutputCache]
public class TagPortalController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IArticlePortalService _articlePortalService;

    public TagPortalController(ITagService tagService, IArticlePortalService articlePortalService)
    {
        _tagService = tagService;
        _articlePortalService = articlePortalService;
    }

    [HttpPost("list")]
    public async Task<Result<List<TagDto>>> GetList([FromBody] TagListRequest? request)
    {
        var result = await _tagService.GetListAsync(request?.Size);
        return Result<List<TagDto>>.Ok(result);
    }

    [HttpPost("select/list")]
    public async Task<Result<List<TagSelectDto>>> GetSelectList([FromBody] PageRequest? request)
    {
        var result = await _tagService.GetSelectListAsync();
        return Result<List<TagSelectDto>>.Ok(result);
    }

    [HttpPost("article/list")]
    public async Task<Result<PageDto<ArticleDto>>> GetTagArticleList([FromBody] TagArticleRequest request)
    {
        var tagId = request.TagId > 0 ? request.TagId : request.Id;
        var pageNum = request.PageNum > 0 ? request.PageNum : (request.Current > 0 ? request.Current : 1);
        var pageSize = request.PageSize > 0 ? request.PageSize : (request.Size > 0 ? request.Size : 10);
        
        var pageRequest = new PageRequest
        {
            PageNum = pageNum,
            PageSize = pageSize
        };
        var result = await _articlePortalService.GetPageByTagAsync(tagId, pageRequest);
        return Result<PageDto<ArticleDto>>.Ok(result);
    }
}

public class TagListRequest
{
    public int? Size { get; set; }
}

public class TagArticleRequest
{
    public long TagId { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public long Id { get; set; } // 兼容前端
    public int Current { get; set; } // 兼容前端
    public int Size { get; set; } // 兼容前端
}

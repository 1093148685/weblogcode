using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

/// <summary>
/// 文章搜索控制器
/// </summary>
[Route("api/search")]
[ApiController]
[OutputCache]
public class SearchController : ControllerBase
{
    private readonly IArticlePortalService _articlePortalService;

    public SearchController(IArticlePortalService articlePortalService)
    {
        _articlePortalService = articlePortalService;
    }

    [HttpPost("article")]
    public async Task<Result<PageDto<ArticleDto>>> SearchArticle([FromBody] SearchArticleRequest request)
    {
        var keyword = !string.IsNullOrEmpty(request.Keyword) ? request.Keyword : (request.Word ?? "");
        var pageNum = request.PageNum > 0 ? request.PageNum : (request.Current > 0 ? request.Current : 1);
        var pageSize = request.PageSize > 0 ? request.PageSize : (request.Size > 0 ? request.Size : 10);
        
        var pageRequest = new PageRequest
        {
            PageNum = pageNum,
            PageSize = pageSize
        };
        var result = await _articlePortalService.GetPageByKeywordAsync(keyword, pageRequest);
        return Result<PageDto<ArticleDto>>.Ok(result);
    }
}

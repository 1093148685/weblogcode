using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/giphy")]
public class GiphyController : ControllerBase
{
    private readonly IGiphyService _giphyService;

    public GiphyController(IGiphyService giphyService)
    {
        _giphyService = giphyService;
    }

    [HttpGet("search")]
    public async Task<Result<List<GiphyItemDto>>> Search([FromQuery] string q, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Result<List<GiphyItemDto>>.Fail("搜索关键词不能为空");

        var results = await _giphyService.SearchAsync(q, limit, offset);
        return Result<List<GiphyItemDto>>.Ok(results);
    }

    [HttpGet("trending")]
    public async Task<Result<List<GiphyItemDto>>> Trending([FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        var results = await _giphyService.TrendingAsync(limit, offset);
        return Result<List<GiphyItemDto>>.Ok(results);
    }
}
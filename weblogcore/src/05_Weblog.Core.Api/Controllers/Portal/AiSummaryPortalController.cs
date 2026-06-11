using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;
using Weblog.Core.Api.Services;
using System.Text;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/ai-summary")]
[ApiController]
public class AiSummaryPortalController : ControllerBase
{
    private readonly IAiSummaryService _aiSummaryService;
    private readonly InMemoryCacheService _cache;

    public AiSummaryPortalController(IAiSummaryService aiSummaryService, InMemoryCacheService cache)
    {
        _aiSummaryService = aiSummaryService;
        _cache = cache;
    }

    [HttpGet("{articleId:long}")]
    public async Task<Result<AiSummaryDto>> Get(long articleId)
    {
        var cacheKey = $"ai_summary_{articleId}";
        
        // 尝试从缓存获取
        var cached = _cache.Get<AiSummaryDto>(cacheKey);
        if (cached != null)
        {
            return Result<AiSummaryDto>.Ok(cached);
        }

        // 从数据库获取
        var result = await _aiSummaryService.GetByArticleIdAsync(articleId);
        
        // 如果有数据，存入缓存
        if (result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1)); // 缓存1小时
        }
        
        return Result<AiSummaryDto>.Ok(result ?? new AiSummaryDto());
    }

    [HttpGet("stream/{articleId:long}")]
    public async Task Stream(long articleId)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var cacheKey = $"ai_summary_{articleId}";
        
        // 尝试从缓存获取
        var cached = _cache.Get<AiSummaryDto>(cacheKey);
        string content = cached?.Content ?? "";
        
        if (!string.IsNullOrEmpty(content))
        {
            // 将内容分成多个块输出，每块几个字
            int chunkSize = 3;
            for (int i = 0; i < content.Length; i += chunkSize)
            {
                int len = Math.Min(chunkSize, content.Length - i);
                string chunk = content.Substring(i, len);
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
                await Task.Delay(50);
            }
        }
        
        await Response.WriteAsync("data: [DONE]\n\n");
        await Response.Body.FlushAsync();
    }
}

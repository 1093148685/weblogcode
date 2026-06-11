using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/link-preview")]
[ApiController]
public class LinkPreviewController : ControllerBase
{
    private readonly ILinkPreviewService _linkPreviewService;
    private readonly ILogger<LinkPreviewController> _logger;

    public LinkPreviewController(ILinkPreviewService linkPreviewService, ILogger<LinkPreviewController> logger)
    {
        _linkPreviewService = linkPreviewService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<Result<LinkPreviewDto>> GetPreview([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result<LinkPreviewDto>.Fail("URL不能为空");
        }

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }

        try
        {
            var preview = await _linkPreviewService.GetPreviewAsync(url);
            if (preview == null)
            {
                return Result<LinkPreviewDto>.Fail("无法获取链接预览或链接不在白名单中");
            }

            return Result<LinkPreviewDto>.Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取链接预览失败: {Url}", url);
            return Result<LinkPreviewDto>.Fail("获取链接预览失败");
        }
    }

    [HttpDelete("cache")]
    public async Task<Result> ClearCache()
    {
        try
        {
            await _linkPreviewService.ClearCacheAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除缓存失败");
            return Result.Fail("清除缓存失败");
        }
    }
}
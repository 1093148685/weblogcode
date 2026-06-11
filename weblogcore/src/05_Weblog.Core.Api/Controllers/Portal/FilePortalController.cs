using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Services;
using Weblog.Core.Common.Result;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[ApiController]
[Route("api/comment/file")]
public class FilePortalController : ControllerBase
{
    private const int DefaultImageMaxSizeMb = 5;
    private const int HardImageMaxSizeMb = 20;
    private const long HardImageMaxSizeBytes = (long)HardImageMaxSizeMb * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private readonly IBlogSettingsService _blogSettingsService;
    private readonly ILogger<FilePortalController> _logger;
    private readonly MinIOService _minIOService;

    public FilePortalController(
        IBlogSettingsService blogSettingsService,
        ILogger<FilePortalController> logger,
        MinIOService minIOService)
    {
        _blogSettingsService = blogSettingsService;
        _logger = logger;
        _minIOService = minIOService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(HardImageMaxSizeBytes + 1024 * 1024)]
    public async Task<Result<string>> Upload([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Result<string>.Fail("璇烽€夋嫨鏂囦欢");
        }

        var maxSizeMb = await GetImageMaxSizeMbAsync();
        var maxSizeBytes = (long)maxSizeMb * 1024 * 1024;

        if (request.File.Length > maxSizeBytes)
        {
            return Result<string>.Fail($"鍥剧墖涓嶈兘瓒呰繃 {maxSizeMb}MB锛岃鍘嬬缉鍚庡啀涓婁紶");
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return Result<string>.Fail("鍙厑璁镐笂浼犲浘鐗囨枃浠?");
        }

        if (!string.IsNullOrWhiteSpace(request.File.ContentType) &&
            !request.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Fail("鍙敮鎸佷笂浼犲浘鐗囨枃浠?");
        }

        try
        {
            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            ms.Position = 0;

            var url = await _minIOService.UploadFileAsync("comments", request.File.FileName, ms);
            return Result<string>.Ok(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "璇勮鍥剧墖涓婁紶澶辫触: {FileName}, Size={Size}", request.File.FileName, request.File.Length);
            return Result<string>.Fail($"涓婁紶澶辫触: {ex.Message}");
        }
    }

    private async Task<int> GetImageMaxSizeMbAsync()
    {
        try
        {
            var settings = await _blogSettingsService.GetAsync();
            var configuredSize = settings?.CommentImageMaxSizeMb ?? DefaultImageMaxSizeMb;
            if (configuredSize <= 0)
            {
                return DefaultImageMaxSizeMb;
            }

            return Math.Clamp(configuredSize, 1, HardImageMaxSizeMb);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "璇诲彇鍥剧墖涓婁紶澶у皬璁剧疆澶辫触锛屼娇鐢ㄩ粯璁ら檺鍒?");
            return DefaultImageMaxSizeMb;
        }
    }
}

public class FileUploadRequest
{
    public IFormFile? File { get; set; }
}

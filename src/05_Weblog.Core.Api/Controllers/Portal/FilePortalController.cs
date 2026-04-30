using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Services;
using Weblog.Core.Common.Result;

namespace Weblog.Core.Api.Controllers.Portal;

[ApiController]
[Route("api/comment/file")]
public class FilePortalController : ControllerBase
{
    private const long MaxImageSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private readonly MinIOService _minIOService;
    private readonly ILogger<FilePortalController> _logger;

    public FilePortalController(MinIOService minIOService, ILogger<FilePortalController> logger)
    {
        _minIOService = minIOService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxImageSize + 1024 * 1024)]
    public async Task<Result<string>> Upload([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Result<string>.Fail("请选择文件");
        }

        if (request.File.Length > MaxImageSize)
        {
            return Result<string>.Fail("图片大小不能超过 5MB");
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return Result<string>.Fail("只允许上传图片文件");
        }

        var contentType = request.File.ContentType ?? string.Empty;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Fail("文件类型不是有效图片");
        }

        try
        {
            await using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            var url = await _minIOService.UploadFileAsync("comments", request.File.FileName, ms.ToArray());
            return Result<string>.Ok(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comment image upload failed. FileName={FileName}, Length={Length}, ContentType={ContentType}",
                request.File.FileName,
                request.File.Length,
                request.File.ContentType);

            return Result<string>.Fail("图片上传失败，请稍后再试");
        }
    }
}

public class FileUploadRequest
{
    public IFormFile? File { get; set; }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Api.Services;
using Weblog.Core.Common.Result;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/file")]
[ApiController]
[Authorize]
public class FileController : ControllerBase
{
    private const int DefaultImageMaxSizeMb = 5;
    private const int HardImageMaxSizeMb = 20;
    private const long HardImageMaxSizeBytes = (long)HardImageMaxSizeMb * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".ico"
    };

    private readonly IBlogSettingsService _blogSettingsService;
    private readonly ILogger<FileController> _logger;
    private readonly MinIOService _minIOService;

    public FileController(
        IBlogSettingsService blogSettingsService,
        ILogger<FileController> logger,
        MinIOService minIOService)
    {
        _blogSettingsService = blogSettingsService;
        _logger = logger;
        _minIOService = minIOService;
    }

    [HttpPost("upload")]
    [RequireRole("admin")]
    [RequestSizeLimit(HardImageMaxSizeBytes + 1024 * 1024)]
    public async Task<Result<string>> Upload([FromForm] IFormFile? file, [FromForm] string? folder)
    {
        if (file == null || file.Length == 0)
        {
            return Result<string>.Fail("请选择要上传的图片");
        }

        var maxSizeMb = await GetImageMaxSizeMbAsync();
        var maxSizeBytes = (long)maxSizeMb * 1024 * 1024;

        if (file.Length > maxSizeBytes)
        {
            return Result<string>.Fail($"图片不能超过 {maxSizeMb}MB，请压缩后再上传");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return Result<string>.Fail("仅支持 jpg、png、gif、webp、bmp、svg、ico 格式图片");
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Fail("仅支持上传图片文件");
        }

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var uploadFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder;
            var url = await _minIOService.UploadFileAsync(uploadFolder, file.FileName, ms);
            return Result<string>.Ok(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传后台图片失败: {FileName}, Size={Size}", file.FileName, file.Length);
            return Result<string>.Fail("图片上传失败，请稍后再试");
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
            _logger.LogWarning(ex, "读取图片上传大小设置失败，使用默认限制");
            return DefaultImageMaxSizeMb;
        }
    }
}

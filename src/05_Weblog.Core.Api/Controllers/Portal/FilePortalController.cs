using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Services;
using Weblog.Core.Common.Result;

namespace Weblog.Core.Api.Controllers.Portal;

[ApiController]
[Route("api/comment/file")]
public class FilePortalController : ControllerBase
{
    private readonly MinIOService _minIOService;

    public FilePortalController(MinIOService minIOService)
    {
        _minIOService = minIOService;
    }

    [HttpPost("upload")]
    public async Task<Result<string>> Upload([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Result<string>.Fail("请选择文件");
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        var extension = Path.GetExtension(request.File.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return Result<string>.Fail("只允许上传图片文件");
        }

        // Validate file size (max 5MB)
        if (request.File.Length > 5 * 1024 * 1024)
        {
            return Result<string>.Fail("图片大小不能超过 5MB");
        }

        try
        {
            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            var fileData = ms.ToArray();

            var url = await _minIOService.UploadFileAsync("comments", request.File.FileName, fileData);
            return Result<string>.Ok(url);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"上传失败: {ex.Message}");
        }
    }
}

public class FileUploadRequest
{
    public IFormFile? File { get; set; }
}

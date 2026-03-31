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
    private readonly IBlogSettingsService _blogSettingsService;
    private readonly MinIOService _minIOService;

    public FileController(IBlogSettingsService blogSettingsService, MinIOService minIOService)
    {
        _blogSettingsService = blogSettingsService;
        _minIOService = minIOService;
    }

    [HttpPost("upload")]
    [RequireRole("admin")]
    public async Task<Result<string>> Upload([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Result<string>.Fail("请选择文件");
        }

        try
        {
            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            var fileData = ms.ToArray();

            var folder = request.Folder ?? "uploads";
            
            // Use MinIO to upload
            var url = await _minIOService.UploadFileAsync(folder, request.File.FileName, fileData);
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
    public string? Folder { get; set; }
}

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
    public async Task<Result<string>> Upload([FromForm] IFormFile? file, [FromForm] string? folder)
    {
        if (file == null || file.Length == 0)
        {
            return Result<string>.Fail("请选择文件");
        }

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var uploadFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder;
            var url = await _minIOService.UploadFileAsync(uploadFolder, file.FileName, ms.ToArray());
            return Result<string>.Ok(url);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"上传失败: {ex.Message}");
        }
    }
}

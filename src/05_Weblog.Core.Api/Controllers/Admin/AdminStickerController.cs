using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/sticker")]
[Authorize]
public class AdminStickerController : ControllerBase
{
    private readonly IStickerService _stickerService;

    public AdminStickerController(IStickerService stickerService)
    {
        _stickerService = stickerService;
    }

    [HttpPost("packs")]
    public async Task<Result<StickerPackDto>> CreatePack([FromBody] CreateStickerPackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<StickerPackDto>.Fail("贴纸包名称不能为空");

        var pack = await _stickerService.CreatePackAsync(request);
        return Result<StickerPackDto>.Ok(pack);
    }

    [HttpGet("packs")]
    public async Task<Result<List<StickerPackDto>>> GetAllPacks()
    {
        var packs = await _stickerService.GetAllPacksIncludingInactiveAsync();
        return Result<List<StickerPackDto>>.Ok(packs);
    }

    [HttpPut("packs/{id}")]
    public async Task<Result<StickerPackDto>> UpdatePack(long id, [FromBody] UpdateStickerPackRequest request)
    {
        var pack = await _stickerService.UpdatePackAsync(id, request);
        if (pack == null)
            return Result<StickerPackDto>.Fail("贴纸包不存在");
        return Result<StickerPackDto>.Ok(pack);
    }

    [HttpDelete("packs/{id}")]
    public async Task<Result<bool>> DeletePack(long id)
    {
        var success = await _stickerService.DeletePackAsync(id);
        if (!success)
            return Result<bool>.Fail("删除失败");
        return Result<bool>.Ok(true);
    }

    [HttpPost("packs/{id}/upload")]
    public async Task<Result<List<StickerDto>>> UploadZip(long id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Result<List<StickerDto>>.Fail("请选择文件");

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (extension != ".zip")
            return Result<List<StickerDto>>.Fail("请上传ZIP文件");

        try
        {
            using var stream = file.OpenReadStream();
            
            // 验证是否是有效的ZIP文件
            if (stream.Length < 4)
                return Result<List<StickerDto>>.Fail("文件无效");
            
            var header = new byte[4];
            stream.Read(header, 0, 4);
            stream.Position = 0;
            
            // ZIP文件魔术字节: 50 4B 03 04
            if (header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
                return Result<List<StickerDto>>.Fail("文件格式不正确，不是有效的ZIP文件");

            var stickers = await _stickerService.UploadStickersFromZipAsync(id, stream, file.FileName);
            return Result<List<StickerDto>>.Ok(stickers);
        }
        catch (Exception ex)
        {
            return Result<List<StickerDto>>.Fail(ex.Message);
        }
    }

    [HttpDelete("stickers/{id}")]
    public async Task<Result<bool>> DeleteSticker(long id)
    {
        var success = await _stickerService.DeleteStickerAsync(id);
        if (!success)
            return Result<bool>.Fail("删除失败");
        return Result<bool>.Ok(true);
    }

    [HttpPost("packs/{packId}/cover/{stickerId}")]
    public async Task<Result<bool>> SetCover(long packId, long stickerId)
    {
        var success = await _stickerService.SetCoverAsync(packId, stickerId);
        if (!success)
            return Result<bool>.Fail("设置封面失败");
        return Result<bool>.Ok(true);
    }
}
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Portal;

[Route("api/sticker")]
public class StickerController : ControllerBase
{
    private readonly IStickerService _stickerService;

    public StickerController(IStickerService stickerService)
    {
        _stickerService = stickerService;
    }

    [HttpGet("packs")]
    public async Task<Result<List<StickerPackDto>>> GetAllPacks()
    {
        var packs = await _stickerService.GetAllPacksAsync();
        return Result<List<StickerPackDto>>.Ok(packs);
    }

    [HttpGet("packs/{id}")]
    public async Task<Result<StickerPackDto>> GetPack(long id)
    {
        var pack = await _stickerService.GetPackByIdAsync(id);
        if (pack == null)
            return Result<StickerPackDto>.Fail("贴纸包不存在");
        return Result<StickerPackDto>.Ok(pack);
    }
}
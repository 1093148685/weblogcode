using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IStickerService
{
    Task<List<StickerPackDto>> GetAllPacksAsync();
    Task<List<StickerPackDto>> GetAllPacksIncludingInactiveAsync();
    Task<StickerPackDto?> GetPackByIdAsync(long id);
    Task<StickerPackDto> CreatePackAsync(CreateStickerPackRequest request);
    Task<StickerPackDto?> UpdatePackAsync(long id, UpdateStickerPackRequest request);
    Task<bool> DeletePackAsync(long id);
    Task<List<StickerDto>> UploadStickersFromZipAsync(long packId, Stream zipStream, string fileName);
    Task<bool> DeleteStickerAsync(long stickerId);
    Task<bool> SetCoverAsync(long packId, long stickerId);
}
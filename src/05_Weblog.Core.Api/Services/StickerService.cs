using System.IO.Compression;
using System.Security.Cryptography;
using Mapster;
using Microsoft.Extensions.Logging;
using Weblog.Core.Api.Services;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class StickerService : IStickerService
{
    private readonly DbContext _dbContext;
    private readonly MinIOService _minIOService;
    private readonly IBlogSettingsService _blogSettingsService;
    private readonly ILogger<StickerService> _logger;
    private static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".webm", ".mp4" };
    private const long MaxFileSize = 10 * 1024 * 1024;

    public StickerService(
        DbContext dbContext,
        MinIOService minIOService,
        IBlogSettingsService blogSettingsService,
        ILogger<StickerService> logger)
    {
        _dbContext = dbContext;
        _minIOService = minIOService;
        _blogSettingsService = blogSettingsService;
        _logger = logger;
    }

    private async Task<int> GetMaxStickersPerPackAsync()
    {
        var settings = await _blogSettingsService.GetAsync();
        return settings.StickerZipMaxCount > 0 ? settings.StickerZipMaxCount : 100;
    }

    public async Task<List<StickerPackDto>> GetAllPacksAsync()
    {
        var packs = await _dbContext.StickerPackDb
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync();

        var result = new List<StickerPackDto>();
        foreach (var pack in packs)
        {
            var dto = pack.Adapt<StickerPackDto>();
            dto.Categories = await GetCategoriesByPackIdAsync(pack.Id);
            result.Add(dto);
        }

        return result;
    }

    public async Task<List<StickerPackDto>> GetAllPacksIncludingInactiveAsync()
    {
        var packs = await _dbContext.StickerPackDb
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync();

        var result = new List<StickerPackDto>();
        foreach (var pack in packs)
        {
            var dto = pack.Adapt<StickerPackDto>();
            dto.Categories = await GetCategoriesByPackIdAsync(pack.Id);
            result.Add(dto);
        }

        return result;
    }

    public async Task<StickerPackDto?> GetPackByIdAsync(long id)
    {
        var pack = await _dbContext.StickerPackDb.Where(x => x.Id == id).FirstAsync();
        if (pack == null)
        {
            return null;
        }

        var dto = pack.Adapt<StickerPackDto>();
        dto.Categories = await GetCategoriesByPackIdAsync(id);
        return dto;
    }

    private async Task<List<StickerCategoryDto>> GetCategoriesByPackIdAsync(long packId)
    {
        var stickers = await _dbContext.StickerDb
            .Where(x => x.PackId == packId)
            .OrderBy(x => x.Category)
            .ToListAsync();

        _logger.LogInformation("Get sticker categories, PackId={PackId}, Count={Count}", packId, stickers.Count);

        return stickers
            .GroupBy(x => x.Category ?? "默认")
            .Select(g => new StickerCategoryDto
            {
                Category = g.Key,
                Stickers = g.Select(s => s.Adapt<StickerDto>()).ToList()
            })
            .ToList();
    }

    public async Task<StickerPackDto> CreatePackAsync(CreateStickerPackRequest request)
    {
        var pack = new StickerPack
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(pack).ExecuteReturnIdentityAsync();
        pack.Id = id;

        return pack.Adapt<StickerPackDto>();
    }

    public async Task<StickerPackDto?> UpdatePackAsync(long id, UpdateStickerPackRequest request)
    {
        var pack = await _dbContext.StickerPackDb.Where(x => x.Id == id).FirstAsync();
        if (pack == null) return null;

        if (request.Name != null) pack.Name = request.Name;
        if (request.Description != null) pack.Description = request.Description;
        if (request.Icon != null) pack.Icon = request.Icon;
        if (request.IsActive.HasValue) pack.IsActive = request.IsActive.Value;

        await _dbContext.Db.Updateable(pack).ExecuteCommandAsync();

        return await GetPackByIdAsync(id);
    }

    public async Task<bool> DeletePackAsync(long id)
    {
        var count = await _dbContext.StickerDb.Where(x => x.PackId == id).CountAsync();
        if (count > 0)
        {
            await _dbContext.Db.Deleteable<Sticker>().Where(x => x.PackId == id).ExecuteCommandAsync();
        }

        return await _dbContext.Db.Deleteable<StickerPack>().Where(x => x.Id == id).ExecuteCommandAsync() > 0;
    }

    public async Task<List<StickerDto>> UploadStickersFromZipAsync(long packId, Stream zipStream, string fileName)
    {
        _logger.LogInformation("Start sticker pack upload. PackId={PackId}, FileName={FileName}", packId, fileName);

        var pack = await _dbContext.StickerPackDb.Where(x => x.Id == packId).FirstAsync();
        if (pack == null)
        {
            throw new Exception("贴纸包不存在");
        }

        var maxCount = await GetMaxStickersPerPackAsync();
        var uploadedStickers = new List<StickerDto>();

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        _logger.LogInformation("ZIP entry count: {EntryCount}", archive.Entries.Count);

        var imageEntries = archive.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name) && SupportedExtensions.Contains(Path.GetExtension(e.Name).ToLower()))
            .ToList();

        _logger.LogInformation("Image entry count: {ImageCount}", imageEntries.Count);

        var filesToUpload = new List<(ZipArchiveEntry Entry, string Category, string Hash)>();

        foreach (var entry in imageEntries)
        {
            try
            {
                var category = GetCategoryFromPath(entry.FullName);
                using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms);

                var fileLength = ms.Length;
                if (fileLength > MaxFileSize)
                {
                    _logger.LogWarning("Sticker file too large: {EntryName}, Size={Size}, Limit={Limit}", entry.Name, fileLength, MaxFileSize);
                    continue;
                }

                var hash = ComputeSha256Hash(ms.GetBuffer().AsSpan(0, (int)fileLength));

                var existingSticker = await _dbContext.StickerDb
                    .Where(x => x.PackId == packId && x.ContentHash == hash)
                    .FirstAsync();

                if (existingSticker != null)
                {
                    _logger.LogInformation("Sticker already exists, skipping: {EntryName}, Hash={Hash}, ExistingId={Id}", entry.Name, hash, existingSticker.Id);
                    uploadedStickers.Add(existingSticker.Adapt<StickerDto>());
                    continue;
                }

                filesToUpload.Add((entry, category, hash));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sticker {Name}", entry.Name);
            }
        }

        var currentCount = await _dbContext.StickerDb.Where(x => x.PackId == packId).CountAsync();
        var newStickersCount = filesToUpload.Count;

        if (currentCount + newStickersCount > maxCount)
        {
            throw new Exception($"上传后贴纸数量将超过限制({maxCount}张，当前{currentCount}张，新增{newStickersCount}张)");
        }

        foreach (var (entry, category, hash) in filesToUpload)
        {
            try
            {
                using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms);
                ms.Position = 0;

                _logger.LogInformation("Preparing sticker upload: {EntryName}, Size={Size}, Category={Category}, Hash={Hash}", entry.Name, ms.Length, category, hash);

                var fileNameOnMinio = $"{packId}/{Guid.NewGuid()}{Path.GetExtension(entry.Name)}";
                var imageUrl = await UploadToMinIOAsync(ms, fileNameOnMinio);

                _logger.LogInformation("Sticker upload completed: {EntryName}, Url={Url}", entry.Name, imageUrl);

                var isAnimated = Path.GetExtension(entry.Name).ToLower() is ".gif" or ".webm" or ".mp4";

                var sticker = new Sticker
                {
                    PackId = packId,
                    Category = category,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = imageUrl,
                    IsAnimated = isAnimated,
                    ContentHash = hash
                };

                var id = await _dbContext.Db.Insertable(sticker).ExecuteReturnIdentityAsync();
                sticker.Id = id;

                _logger.LogInformation("Sticker saved. Id={Id}, PackId={PackId}, Hash={Hash}", id, packId, hash);

                uploadedStickers.Add(sticker.Adapt<StickerDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload sticker {Name}", entry.Name);
            }
        }

        _logger.LogInformation(
            "Sticker upload summary: Total={Total}, Uploaded={Uploaded}, Skipped={Skipped}",
            imageEntries.Count,
            uploadedStickers.Count,
            imageEntries.Count - uploadedStickers.Count);

        return uploadedStickers;
    }

    private static string ComputeSha256Hash(ReadOnlySpan<byte> data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private string GetCategoryFromPath(string fullName)
    {
        var parts = fullName.Split('/');
        if (parts.Length > 1)
        {
            return parts[0];
        }

        return "默认";
    }

    private async Task<string> UploadToMinIOAsync(Stream fileStream, string objectName)
    {
        var fileName = objectName.Replace("stickers/", "");
        return await _minIOService.UploadFileAsync("stickers", fileName, fileStream);
    }

    public async Task<bool> DeleteStickerAsync(long stickerId)
    {
        return await _dbContext.Db.Deleteable<Sticker>().Where(x => x.Id == stickerId).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> SetCoverAsync(long packId, long stickerId)
    {
        var sticker = await _dbContext.StickerDb
            .Where(x => x.Id == stickerId && x.PackId == packId)
            .FirstAsync();

        if (sticker == null) return false;

        var pack = await _dbContext.StickerPackDb.Where(x => x.Id == packId).FirstAsync();
        if (pack == null) return false;

        pack.Icon = sticker.ThumbnailUrl ?? sticker.ImageUrl;
        await _dbContext.Db.Updateable(pack).ExecuteCommandAsync();

        return true;
    }
}

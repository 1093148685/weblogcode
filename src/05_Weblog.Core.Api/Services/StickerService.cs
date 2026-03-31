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
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public StickerService(DbContext dbContext, MinIOService minIOService, IBlogSettingsService blogSettingsService, ILogger<StickerService> logger)
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
        if (pack == null) return null;

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
        
        _logger.LogInformation("获取贴纸分类, PackId={PackId}, 贴纸数量={Count}", packId, stickers.Count);

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
        _logger.LogInformation("开始上传贴纸包: PackId={PackId}, FileName={FileName}", packId, fileName);
        
        var pack = await _dbContext.StickerPackDb.Where(x => x.Id == packId).FirstAsync();
        if (pack == null)
            throw new Exception("贴纸包不存在");

        var maxCount = await GetMaxStickersPerPackAsync();
        
        var uploadedStickers = new List<StickerDto>();

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        _logger.LogInformation("ZIP文件条目数: {EntryCount}", archive.Entries.Count);
        
        var imageEntries = archive.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name) && SupportedExtensions.Contains(Path.GetExtension(e.Name).ToLower()))
            .ToList();
        
        _logger.LogInformation("符合条件的图片数量: {ImageCount}", imageEntries.Count);

        // 第一遍：计算哈希，检查重复，确定需要上传的新文件
        var filesToUpload = new List<(ZipArchiveEntry Entry, string Category, string Hash)>();
        
        foreach (var entry in imageEntries)
        {
            try
            {
                var category = GetCategoryFromPath(entry.FullName);
                using var stream = entry.Open();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var fileData = ms.ToArray();
                
                if (fileData.Length > MaxFileSize)
                {
                    _logger.LogWarning("贴纸文件过大: {EntryName}, 大小: {Size}, 限制: {MaxSize}", entry.Name, fileData.Length, MaxFileSize);
                    continue;
                }
                
                var hash = ComputeSha256Hash(fileData);
                
                // 检查是否已存在相同哈希的贴纸
                var existingSticker = await _dbContext.StickerDb
                    .Where(x => x.PackId == packId && x.ContentHash == hash)
                    .FirstAsync();
                
                if (existingSticker != null)
                {
                    _logger.LogInformation("贴纸已存在，跳过: {EntryName}, Hash: {Hash}, ExistingId: {Id}", entry.Name, hash, existingSticker.Id);
                    uploadedStickers.Add(existingSticker.Adapt<StickerDto>());
                    continue;
                }
                
                filesToUpload.Add((entry, category, hash));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理贴纸失败 {Name}", entry.Name);
            }
        }
        
        // 检查上传后是否超过限制
        var currentCount = await _dbContext.StickerDb.Where(x => x.PackId == packId).CountAsync();
        var newStickersCount = filesToUpload.Count;
        
        if (currentCount + newStickersCount > maxCount)
        {
            throw new Exception($"上传后贴纸数量将超过限制({maxCount}张，当前{currentCount}张，新增{newStickersCount}张)");
        }
        
        // 第二遍：上传新文件
        foreach (var (entry, category, hash) in filesToUpload)
        {
            try
            {
                using var stream = entry.Open();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var fileData = ms.ToArray();
                
                _logger.LogInformation("准备上传贴纸: {EntryName}, 大小: {Size}, 分类: {Category}, Hash: {Hash}", entry.Name, fileData.Length, category, hash);

                var fileNameOnMinio = $"{packId}/{Guid.NewGuid()}{Path.GetExtension(entry.Name)}";
                var imageUrl = await UploadToMinIOAsync(fileData, fileNameOnMinio);
                
                _logger.LogInformation("贴纸上传统功: {EntryName}, URL: {Url}", entry.Name, imageUrl);

                var isAnimated = Path.GetExtension(entry.Name).ToLower() == ".gif" || Path.GetExtension(entry.Name).ToLower() == ".webm" || Path.GetExtension(entry.Name).ToLower() == ".mp4";

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
                
                _logger.LogInformation("贴纸已保存到数据库: Id={Id}, PackId={PackId}, Hash: {Hash}", id, packId, hash);

                uploadedStickers.Add(sticker.Adapt<StickerDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传贴纸失败 {Name}", entry.Name);
            }
        }
        
        _logger.LogInformation("贴纸上传统计: 共{ImageCount}张, 新增{NewCount}张, 跳过重复{SkipCount}张", 
            imageEntries.Count, uploadedStickers.Count, imageEntries.Count - uploadedStickers.Count);

        return uploadedStickers;
    }
    
    private static string ComputeSha256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
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

    private async Task<string> UploadToMinIOAsync(byte[] fileData, string objectName)
    {
        var extension = Path.GetExtension(objectName);
        var contentType = extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".webm" => "video/webm",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };

        var fileName = objectName.Replace("stickers/", "");
        return await _minIOService.UploadFileAsync("stickers", fileName, fileData);
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
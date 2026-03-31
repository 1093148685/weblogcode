namespace Weblog.Core.Model.DTOs;

public class StickerPackDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateTime { get; set; }
    public List<StickerCategoryDto> Categories { get; set; } = new();
}

public class StickerCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public List<StickerDto> Stickers { get; set; } = new();
}

public class StickerDto
{
    public long Id { get; set; }
    public long PackId { get; set; }
    public string? Category { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool IsAnimated { get; set; }
}

public class CreateStickerPackRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateStickerPackRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool? IsActive { get; set; }
}

public class GiphyItemDto
{
    public string GiphyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}

public class GiphySearchRequest
{
    public string? Q { get; set; }
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}
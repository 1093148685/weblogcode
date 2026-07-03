using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_sticker")]
public class Sticker
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public long PackId { get; set; }

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    [SugarColumn(Length = 500)]
    public string ImageUrl { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? ThumbnailUrl { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public bool IsAnimated { get; set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? ContentHash { get; set; }

    [SugarColumn(IsIgnore = true)]
    public StickerPack? Pack { get; set; }
}
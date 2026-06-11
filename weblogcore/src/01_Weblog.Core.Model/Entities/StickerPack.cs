using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_sticker_pack")]
public class StickerPack
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 50)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    [SugarColumn(IsIgnore = true)]
    public List<Sticker>? Stickers { get; set; }
}
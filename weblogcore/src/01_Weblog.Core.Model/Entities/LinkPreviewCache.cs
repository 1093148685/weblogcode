using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_link_preview_cache")]
public class LinkPreviewCache
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 2000)]
    public string Url { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string Title { get; set; } = string.Empty;

    [SugarColumn(Length = 1000, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? ImageUrl { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? FaviconUrl { get; set; }

    [SugarColumn(Length = 200)]
    public string Domain { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

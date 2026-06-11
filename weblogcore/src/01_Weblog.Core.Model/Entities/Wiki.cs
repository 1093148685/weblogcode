using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_wiki")]
public class Wiki
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(Length = 120)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 封面
    /// </summary>
    [SugarColumn(Length = 120)]
    public string Cover { get; set; } = string.Empty;

    /// <summary>
    /// 摘要
    /// </summary>
    [SugarColumn(Length = 160, IsNullable = true)]
    public string? Summary { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后一次更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 删除标志位：0：未删除 1：已删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 权重，用于是否置顶（0: 未置顶；>0: 参与置顶，权重值越高越靠前）
    /// </summary>
    public int Weight { get; set; } = 0;

    /// <summary>
    /// 是否发布：0：未发布 1：已发布
    /// </summary>
    public bool IsPublish { get; set; } = true;
}

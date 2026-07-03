using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_tag")]
public class Tag
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后一次更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 逻辑删除标志位：0：未删除 1：已删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 此标签下文章总数
    /// </summary>
    public int ArticlesTotal { get; set; } = 0;
}

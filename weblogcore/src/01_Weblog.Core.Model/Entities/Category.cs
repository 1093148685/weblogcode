using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_category")]
public class Category
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Name { get; set; }

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
    /// 此分类下文章总数
    /// </summary>
    public int ArticlesTotal { get; set; } = 0;
}

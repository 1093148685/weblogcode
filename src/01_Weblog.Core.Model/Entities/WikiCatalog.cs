using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_wiki_catalog")]
public class WikiCatalog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 知识库id
    /// </summary>
    public long WikiId { get; set; }

    /// <summary>
    /// 文章id
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public long? ArticleId { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 目录层级
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// 父目录id
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public long? ParentId { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 1;

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
}

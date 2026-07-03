using SqlSugar;

namespace Weblog.Core.Model.Entities;

/// <summary>
/// 文章分类关联表
/// </summary>
[SugarTable("t_article_category_rel")]
[SugarIndex("idx_article_id", nameof(ArticleId), OrderByType.Asc)]
[SugarIndex("idx_category_id", nameof(CategoryId), OrderByType.Asc)]
public class ArticleCategoryRel
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 文章ID
    /// </summary>
    [SugarColumn(ColumnName = "ArticleId")]
    public long ArticleId { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    [SugarColumn(ColumnName = "CategoryId")]
    public long CategoryId { get; set; }

    [SugarColumn(IsNullable = true, ColumnName = "CreateTime")]
    public DateTime? CreateTime { get; set; }
}

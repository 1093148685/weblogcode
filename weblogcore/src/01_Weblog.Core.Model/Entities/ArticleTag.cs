using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_article_tag")]
[SugarIndex("idx_article_id", nameof(ArticleId), OrderByType.Asc)]
[SugarIndex("idx_tag_id", nameof(TagId), OrderByType.Asc)]
public class ArticleTag
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "ArticleId")]
    public long ArticleId { get; set; }

    [SugarColumn(ColumnName = "TagId")]
    public long TagId { get; set; }
}

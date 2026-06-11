using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_article_tag")]
public class ArticleTag
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "ArticleId")]
    public long ArticleId { get; set; }

    [SugarColumn(ColumnName = "TagId")]
    public long TagId { get; set; }
}

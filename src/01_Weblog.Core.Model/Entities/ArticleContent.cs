using SqlSugar;

namespace Weblog.Core.Model.Entities;

/// <summary>
/// 文章内容表
/// </summary>
[SugarTable("t_article_content")]
public class ArticleContent
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 关联文章ID
    /// </summary>
    public long ArticleId { get; set; }

    /// <summary>
    /// 文章正文内容
    /// </summary>
    [SugarColumn(ColumnDataType = "longtext")]
    public string Content { get; set; } = string.Empty;
}

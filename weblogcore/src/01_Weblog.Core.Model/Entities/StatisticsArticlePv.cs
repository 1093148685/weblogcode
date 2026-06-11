using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_statistics_article_pv")]
public class StatisticsArticlePv
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 被统计的日期
    /// </summary>
    public DateTime PvDate { get; set; }

    /// <summary>
    /// pv浏览量
    /// </summary>
    public long PvCount { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后一次更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

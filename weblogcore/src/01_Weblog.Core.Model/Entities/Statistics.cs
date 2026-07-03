using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_statistics")]
public class Statistics
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 日期
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 浏览量
    /// </summary>
    public long ViewCount { get; set; } = 0;
}

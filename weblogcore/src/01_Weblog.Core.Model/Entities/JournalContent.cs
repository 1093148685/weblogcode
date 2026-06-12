using SqlSugar;

namespace Weblog.Core.Model.Entities;

/// <summary>
/// 日志内容表
/// </summary>
[SugarTable("t_journal_content")]
public class JournalContent
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 关联日志ID
    /// </summary>
    public long JournalId { get; set; }

    /// <summary>
    /// 日志正文内容
    /// </summary>
    [SugarColumn(ColumnDataType = "longtext")]
    public string Content { get; set; } = string.Empty;
}

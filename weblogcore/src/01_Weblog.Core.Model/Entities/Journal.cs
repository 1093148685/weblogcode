using SqlSugar;

namespace Weblog.Core.Model.Entities;

/// <summary>
/// 日志表
/// </summary>
[SugarTable("t_journal")]
public class Journal
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 日志标题
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Title { get; set; } = string.Empty;

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

    /// <summary>
    /// 被阅读次数
    /// </summary>
    public int ReadNum { get; set; } = 0;
}

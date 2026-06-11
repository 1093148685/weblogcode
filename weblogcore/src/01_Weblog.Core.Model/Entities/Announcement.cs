using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_announcement")]
public class Announcement
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 公告内容
    /// </summary>
    [SugarColumn(Length = 1000, DefaultValue = "")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否删除
    /// </summary>
    [SugarColumn(DefaultValue = "0")]
    public bool IsDeleted { get; set; } = false;
}

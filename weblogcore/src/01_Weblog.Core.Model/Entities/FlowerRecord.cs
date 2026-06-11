using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_flower_record")]
public class FlowerRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 评论ID
    /// </summary>
    public long CommentId { get; set; }

    /// <summary>
    /// 用户标识（IP或UserId）
    /// </summary>
    [SugarColumn(Length = 64)]
    public string UserKey { get; set; } = string.Empty;

    /// <summary>
    /// 送花时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;
}
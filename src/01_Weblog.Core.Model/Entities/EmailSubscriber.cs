using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_email_subscriber")]
public class EmailSubscriber
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 120)]
    public string Email { get; set; } = string.Empty;

    [SugarColumn(Length = 80, IsNullable = true)]
    public string? IpAddress { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

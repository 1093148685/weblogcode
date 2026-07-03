using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_user_role")]
public class UserRole
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 角色
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;
}

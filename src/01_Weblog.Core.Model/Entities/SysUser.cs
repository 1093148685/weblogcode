using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_user")]
public class SysUser
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 角色：admin-管理员, visitor-访客
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Role { get; set; } = "admin";

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
}

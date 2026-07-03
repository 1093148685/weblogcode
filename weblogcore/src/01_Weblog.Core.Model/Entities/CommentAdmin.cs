using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("comment_admin")]
public class CommentAdmin
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "username", Length = 50)]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "password_hash", Length = 255)]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "nickname", Length = 50, IsNullable = true)]
    public string? Nickname { get; set; }

    [SugarColumn(ColumnName = "token", Length = 255, IsNullable = true)]
    public string? Token { get; set; }

    [SugarColumn(ColumnName = "token_expire_time")]
    public DateTime? TokenExpireTime { get; set; }

    [SugarColumn(ColumnName = "created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [SugarColumn(ColumnName = "updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

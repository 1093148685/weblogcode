using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_comment")]
public class Comment
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [SugarColumn(Length = 1000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 头像
    /// </summary>
    [SugarColumn(Length = 160, IsNullable = true)]
    public string? Avatar { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(Length = 60)]
    public string Mail { get; set; } = string.Empty;

    /// <summary>
    /// 网站地址
    /// </summary>
    [SugarColumn(Length = 60, IsNullable = true)]
    public string? Website { get; set; }

    /// <summary>
    /// 评论所属的路由
    /// </summary>
    [SugarColumn(Length = 60)]
    public string RouterUrl { get; set; } = string.Empty;

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
    /// 回复的评论 ID
    /// </summary>
    public long? ReplyCommentId { get; set; }

    /// <summary>
    /// 父评论 ID
    /// </summary>
    public long? ParentCommentId { get; set; }

    /// <summary>
    /// 原因描述
    /// </summary>
    [SugarColumn(Length = 300, IsNullable = true)]
    public string? Reason { get; set; }

    /// <summary>
    /// 1: 待审核；2：正常；3：审核未通过
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 回复的评论昵称
    /// </summary>
    [SugarColumn(Length = 60, IsNullable = true)]
    public string? ReplyNickname { get; set; }

    /// <summary>
    /// 评论者 IP 地址
    /// </summary>
    [SugarColumn(Length = 64, IsNullable = true)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// IP 属地
    /// </summary>
    [SugarColumn(Length = 64, IsNullable = true)]
    public string? IpLocation { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    [SugarColumn(Length = 32, IsNullable = true)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    [SugarColumn(Length = 64, IsNullable = true)]
    public string? Browser { get; set; }

    /// <summary>
    /// 送花数量
    /// </summary>
    public int FlowerCount { get; set; } = 0;

    /// <summary>
    /// 评论图片（多个用逗号分隔）
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    public string? Images { get; set; }

    /// <summary>
    /// 是否是私密内容（马赛克）
    /// </summary>
    public bool IsSecret { get; set; } = false;

    /// <summary>
    /// 加密的私密内容（AES加密）
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? SecretContent { get; set; }

    /// <summary>
    /// 密钥的SHA256哈希（用于验证）
    /// </summary>
    [SugarColumn(Length = 255, IsNullable = true)]
    public string? SecretKeyHash { get; set; }

    /// <summary>
    /// 私密内容过期时间（为空表示永不过期）
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 私密内容是否已过期
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public bool IsExpired { get; set; } = false;

    /// <summary>
    /// 私密内容是否已被重置（管理员重置后无法再查看原内容）
    /// </summary>
    public bool IsReset { get; set; } = false;

    /// <summary>
    /// 是否为管理员发布的评论
    /// </summary>
    public bool IsAdmin { get; set; } = false;

    /// <summary>
    /// 发布评论的管理员ID（关联comment_admin表）
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public long? CommentAdminId { get; set; }
}

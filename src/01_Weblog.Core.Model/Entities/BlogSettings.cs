using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_blog_settings")]
public class BlogSettings
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 博客Logo
    /// </summary>
    [SugarColumn(Length = 120, DefaultValue = "")]
    public string Logo { get; set; } = string.Empty;

    /// <summary>
    /// 博客名称
    /// </summary>
    [SugarColumn(Length = 60, DefaultValue = "")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 作者名
    /// </summary>
    [SugarColumn(Length = 20, DefaultValue = "")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 介绍语
    /// </summary>
    [SugarColumn(Length = 120, DefaultValue = "")]
    public string Introduction { get; set; } = string.Empty;

    /// <summary>
    /// 作者头像
    /// </summary>
    [SugarColumn(Length = 120, DefaultValue = "")]
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// GitHub 主页访问地址
    /// </summary>
    [SugarColumn(Length = 60, DefaultValue = "")]
    public string GithubHomepage { get; set; } = string.Empty;

    /// <summary>
    /// CSDN 主页访问地址
    /// </summary>
    [SugarColumn(Length = 60, DefaultValue = "")]
    public string CsdnHomepage { get; set; } = string.Empty;

    /// <summary>
    /// Gitee 主页访问地址
    /// </summary>
    [SugarColumn(Length = 60, DefaultValue = "")]
    public string GiteeHomepage { get; set; } = string.Empty;

    /// <summary>
    /// 知乎主页访问地址
    /// </summary>
    [SugarColumn(Length = 60, DefaultValue = "")]
    public string ZhihuHomepage { get; set; } = string.Empty;

    /// <summary>
    /// 博主邮箱地址
    /// </summary>
    [SugarColumn(Length = 60, IsNullable = true)]
    public string? Mail { get; set; }

    /// <summary>
    /// 是否开启评论敏感词过滤, 0:不开启；1：开启
    /// </summary>
    public bool IsCommentSensiWordOpen { get; set; } = true;

    /// <summary>
    /// 敏感词列表，用逗号分隔
    /// </summary>
    [SugarColumn(Length = 500, DefaultValue = "")]
    public string SensitiveWords { get; set; } = string.Empty;

    /// <summary>
    /// 是否开启评论审核, 0: 未开启；1：开启
    /// </summary>
    public bool IsCommentExamineOpen { get; set; } = false;

    /// <summary>
    /// 贴纸包ZIP解压最大张数
    /// </summary>
    public int StickerZipMaxCount { get; set; } = 100;

    /// <summary>
    /// 是否开启评论链接预览
    /// </summary>
    public bool IsLinkPreviewOpen { get; set; } = true;

    /// <summary>
    /// 链接预览域名白名单（换行分隔，支持泛化如 *.example.com）
    /// </summary>
    [SugarColumn(Length = 2000, DefaultValue = "")]
    public string LinkPreviewWhitelist { get; set; } = "";

    /// <summary>
    /// 是否开启邮件通知
    /// </summary>
    public bool IsEmailNotificationOpen { get; set; } = false;

    /// <summary>
    /// SMTP 主机地址
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? SmtpHost { get; set; }

    /// <summary>
    /// SMTP 端口
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP 用户名
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP 密码
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// 是否启用 SSL
    /// </summary>
    public bool SmtpEnableSsl { get; set; } = true;

    /// <summary>
    /// 发件人邮箱
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? SmtpFromEmail { get; set; }

    /// <summary>
    /// 发件人名称
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? SmtpFromName { get; set; }

    /// <summary>
    /// 是否展示侧边栏订阅卡片
    /// </summary>
    public bool IsSubscribeCardOpen { get; set; } = true;

    /// <summary>
    /// 订阅卡片标题
    /// </summary>
    [SugarColumn(Length = 40, DefaultValue = "订阅更新")]
    public string SubscribeTitle { get; set; } = "订阅更新";

    /// <summary>
    /// 订阅卡片说明
    /// </summary>
    [SugarColumn(Length = 120, DefaultValue = "订阅后，最新文章将通过邮件发送给你")]
    public string SubscribeDescription { get; set; } = "订阅后，最新文章将通过邮件发送给你";

    /// <summary>
    /// 订阅输入框占位文案
    /// </summary>
    [SugarColumn(Length = 40, DefaultValue = "输入你的邮箱地址")]
    public string SubscribePlaceholder { get; set; } = "输入你的邮箱地址";

    /// <summary>
    /// 订阅按钮文案
    /// </summary>
    [SugarColumn(Length = 20, DefaultValue = "订阅")]
    public string SubscribeButtonText { get; set; } = "订阅";
}

namespace Weblog.Core.Model.DTOs;

public class BlogSettingsDto
{
    public long Id { get; set; }
    public string Logo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string GithubHomepage { get; set; } = string.Empty;
    public string CsdnHomepage { get; set; } = string.Empty;
    public string GiteeHomepage { get; set; } = string.Empty;
    public string ZhihuHomepage { get; set; } = string.Empty;
    public string? Mail { get; set; }
    public bool IsCommentSensiWordOpen { get; set; }
    public string SensitiveWords { get; set; } = string.Empty;
    public bool IsCommentExamineOpen { get; set; }
    public int StickerZipMaxCount { get; set; } = 100;
    public bool IsLinkPreviewOpen { get; set; } = true;
    public string LinkPreviewWhitelist { get; set; } = string.Empty;
    public bool IsEmailNotificationOpen { get; set; } = false;
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; } = true;
    public string? SmtpFromEmail { get; set; }
    public string? SmtpFromName { get; set; }
    public bool IsSubscribeCardOpen { get; set; } = true;
    public string SubscribeTitle { get; set; } = "订阅更新";
    public string SubscribeDescription { get; set; } = "订阅后，最新文章将通过邮件发送给你";
    public string SubscribePlaceholder { get; set; } = "输入你的邮箱地址";
    public string SubscribeButtonText { get; set; } = "订阅";
}

public class UpdateBlogSettingsRequest
{
    public string Logo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string GithubHomepage { get; set; } = string.Empty;
    public string CsdnHomepage { get; set; } = string.Empty;
    public string GiteeHomepage { get; set; } = string.Empty;
    public string ZhihuHomepage { get; set; } = string.Empty;
    public string? Mail { get; set; }
    public bool IsCommentSensiWordOpen { get; set; }
    public string SensitiveWords { get; set; } = string.Empty;
    public bool IsCommentExamineOpen { get; set; }
    public int StickerZipMaxCount { get; set; } = 100;
    public bool IsLinkPreviewOpen { get; set; } = true;
    public string LinkPreviewWhitelist { get; set; } = string.Empty;
    public bool IsEmailNotificationOpen { get; set; } = false;
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; } = true;
    public string? SmtpFromEmail { get; set; }
    public string? SmtpFromName { get; set; }
    public bool IsSubscribeCardOpen { get; set; } = true;
    public string SubscribeTitle { get; set; } = "订阅更新";
    public string SubscribeDescription { get; set; } = "订阅后，最新文章将通过邮件发送给你";
    public string SubscribePlaceholder { get; set; } = "输入你的邮箱地址";
    public string SubscribeButtonText { get; set; } = "订阅";
}

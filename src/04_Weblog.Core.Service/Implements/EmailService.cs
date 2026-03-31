using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class EmailService : IEmailService
{
    private readonly IBlogSettingsService _blogSettingsService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IBlogSettingsService blogSettingsService, ILogger<EmailService> logger)
    {
        _blogSettingsService = blogSettingsService;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var settings = await _blogSettingsService.GetAsync();
            
            if (!settings.IsEmailNotificationOpen)
            {
                _logger.LogInformation("Email notification is disabled");
                return true;
            }

            if (string.IsNullOrEmpty(settings.SmtpHost) || 
                string.IsNullOrEmpty(settings.SmtpUsername) ||
                string.IsNullOrEmpty(settings.SmtpPassword) ||
                string.IsNullOrEmpty(settings.SmtpFromEmail))
            {
                _logger.LogWarning("SMTP settings are not configured");
                return false;
            }

            using var client = new SmtpClient();
            client.Host = settings.SmtpHost;
            client.Port = settings.SmtpPort;
            client.EnableSsl = settings.SmtpEnableSsl;
            client.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);

            var from = new MailAddress(
                settings.SmtpFromEmail, 
                settings.SmtpFromName ?? settings.Author,
                System.Text.Encoding.UTF8
            );
            var toAddr = new MailAddress(to);

            using var message = new MailMessage();
            message.From = from;
            message.To.Add(toAddr);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;
            message.BodyEncoding = System.Text.Encoding.UTF8;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendCommentNotificationToAdminAsync(Comment comment, string? replyToNickname = null)
    {
        try
        {
            var settings = await _blogSettingsService.GetAsync();
            
            if (!settings.IsEmailNotificationOpen || string.IsNullOrEmpty(settings.Mail))
            {
                return false;
            }

            var subject = string.IsNullOrEmpty(replyToNickname)
                ? $"【新评论】来自 {comment.Nickname}"
                : $"【新回复】{comment.Nickname} 回复了 {replyToNickname}";

            var pageName = GetPageName(comment.RouterUrl);
            var htmlBody = BuildAdminNotificationHtml(comment, subject, pageName, replyToNickname);

            return await SendEmailAsync(settings.Mail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send comment notification to admin");
            return false;
        }
    }

    public async Task<bool> SendReplyNotificationToUserAsync(Comment comment, Comment parentComment)
    {
        try
        {
            var settings = await _blogSettingsService.GetAsync();
            
            if (!settings.IsEmailNotificationOpen || string.IsNullOrEmpty(parentComment.Mail))
            {
                return false;
            }

            if (parentComment.IsAdmin)
            {
                return false;
            }

            var subject = $"【回复通知】{comment.Nickname} 回复了你的评论";
            var pageName = GetPageName(comment.RouterUrl);
            var pageUrl = GetPageUrl(comment.RouterUrl);
            var htmlBody = BuildReplyNotificationHtml(comment, parentComment, subject, pageName, pageUrl);

            return await SendEmailAsync(parentComment.Mail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reply notification to user {Mail}", parentComment.Mail);
            return false;
        }
    }

    private string BuildAdminNotificationHtml(Comment comment, string subject, string pageName, string? replyToNickname)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head><meta charset='UTF-8'></head>");
        sb.AppendLine("<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>");
        sb.AppendLine("    <div style='background: #f5f5f5; border-radius: 8px; padding: 20px;'>");
        sb.AppendLine($"        <h2 style='color: #333; margin-top: 0;'>{subject}</h2>");
        sb.AppendLine("        <div style='background: white; border-radius: 6px; padding: 16px; margin: 16px 0;'>");
        sb.AppendLine("            <div style='display: flex; align-items: center; margin-bottom: 12px;'>");
        sb.AppendLine($"                <img src='{comment.Avatar}' style='width: 40px; height: 40px; border-radius: 50%; margin-right: 12px;' onerror='this.style.display=\"none\"'>");
        sb.AppendLine("                <div>");
        sb.AppendLine($"                    <strong style='color: #333;'>{comment.Nickname}</strong>");
        if (!string.IsNullOrEmpty(comment.IpLocation))
        {
            sb.AppendLine($"                    <span style='color: #999; font-size: 12px;'> · {comment.IpLocation}</span>");
        }
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine($"            <p style='color: #555; line-height: 1.6; margin: 0;'>{comment.Content}</p>");
        if (!string.IsNullOrEmpty(replyToNickname))
        {
            sb.AppendLine($"            <p style='color: #888; font-size: 14px; margin-top: 12px;'>↳ 回复 @{replyToNickname}</p>");
        }
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style='color: #888; font-size: 12px;'>");
        sb.AppendLine($"            <p style='margin: 4px 0;'>页面：{pageName}</p>");
        sb.AppendLine($"            <p style='margin: 4px 0;'>时间：{comment.CreateTime:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"            <p style='margin: 4px 0;'>邮箱：{comment.Mail}</p>");
        if (!string.IsNullOrEmpty(comment.Website))
        {
            sb.AppendLine($"            <p style='margin: 4px 0;'>网站：<a href='{comment.Website}'>{comment.Website}</a></p>");
        }
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private string BuildReplyNotificationHtml(Comment comment, Comment parentComment, string subject, string pageName, string pageUrl)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head><meta charset='UTF-8'></head>");
        sb.AppendLine("<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>");
        sb.AppendLine("    <div style='background: #f5f5f5; border-radius: 8px; padding: 20px;'>");
        sb.AppendLine($"        <h2 style='color: #333; margin-top: 0;'>收到回复啦</h2>");
        sb.AppendLine("        <div style='background: white; border-radius: 6px; padding: 16px; margin: 16px 0;'>");
        sb.AppendLine("            <p style='color: #888; font-size: 14px; margin: 0 0 12px 0;'>你发布的评论：</p>");
        sb.AppendLine($"            <p style='color: #666; line-height: 1.6; margin: 0; padding: 12px; background: #fafafa; border-radius: 4px;'>{parentComment.Content}</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style='background: white; border-radius: 6px; padding: 16px; margin: 16px 0;'>");
        sb.AppendLine("            <div style='display: flex; align-items: center; margin-bottom: 12px;'>");
        sb.AppendLine($"                <img src='{comment.Avatar}' style='width: 40px; height: 40px; border-radius: 50%; margin-right: 12px;' onerror='this.style.display=\"none\"'>");
        sb.AppendLine("                <div>");
        sb.AppendLine($"                    <strong style='color: #333;'>{comment.Nickname}</strong>");
        if (!string.IsNullOrEmpty(comment.IpLocation))
        {
            sb.AppendLine($"                    <span style='color: #999; font-size: 12px;'> · {comment.IpLocation}</span>");
        }
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine($"            <p style='color: #555; line-height: 1.6; margin: 0;'>{comment.Content}</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style='text-align: center; margin-top: 20px;'>");
        sb.AppendLine($"            <a href='{pageUrl}' style='display: inline-block; background: #007bff; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: 500;'>查看回复</a>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div style='color: #888; font-size: 12px; margin-top: 20px; text-align: center;'>");
        sb.AppendLine($"            <p style='margin: 4px 0;'>页面：{pageName}</p>");
        sb.AppendLine($"            <p style='margin: 4px 0;'>时间：{comment.CreateTime:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private string GetPageName(string routerUrl)
    {
        return routerUrl switch
        {
            "/message-wall" => "留言板",
            var url when url.StartsWith("/article/") => "文章详情",
            var url when url.StartsWith("/wiki/") => "知识库",
            _ => string.IsNullOrEmpty(routerUrl) ? "未知页面" : routerUrl
        };
    }

    private string GetPageUrl(string routerUrl)
    {
        var baseUrl = "https://www.qianjinge.com";
        return $"{baseUrl}{routerUrl}";
    }
}

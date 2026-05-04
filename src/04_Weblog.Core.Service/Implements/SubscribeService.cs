using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class SubscribeService : ISubscribeService
{
    private readonly DbContext _dbContext;
    private readonly IBlogSettingsService _blogSettingsService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscribeService> _logger;

    public SubscribeService(
        DbContext dbContext,
        IBlogSettingsService blogSettingsService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SubscribeService> logger)
    {
        _dbContext = dbContext;
        _blogSettingsService = blogSettingsService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SubscribeAsync(string email, string? ipAddress)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(email, out _))
        {
            throw new Exception("请输入正确的邮箱地址");
        }

        var settings = await _blogSettingsService.GetAsync();
        if (!settings.IsEmailNotificationOpen ||
            string.IsNullOrWhiteSpace(settings.SmtpHost) ||
            string.IsNullOrWhiteSpace(settings.SmtpUsername) ||
            string.IsNullOrWhiteSpace(settings.SmtpPassword) ||
            string.IsNullOrWhiteSpace(settings.SmtpFromEmail))
        {
            throw new Exception("邮箱服务未配置，订阅暂不可用");
        }

        var subscriber = await _dbContext.Db.Queryable<EmailSubscriber>()
            .Where(it => it.Email == email)
            .FirstAsync();

        if (subscriber == null)
        {
            subscriber = new EmailSubscriber
            {
                Email = email,
                IpAddress = ipAddress,
                IsActive = true,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _dbContext.Db.Insertable(subscriber).ExecuteCommandAsync();
        }
        else if (!subscriber.IsActive)
        {
            subscriber.IsActive = true;
            subscriber.IpAddress = ipAddress;
            subscriber.UpdateTime = DateTime.Now;
            await _dbContext.Db.Updateable(subscriber).ExecuteCommandAsync();
        }

        var blogName = string.IsNullOrWhiteSpace(settings.Name) ? "博客" : settings.Name;
        var body = $"""
            <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;line-height:1.8;color:#1f2937;">
                <h2 style="margin:0 0 12px;">订阅成功</h2>
                <p>你已成功订阅 <strong>{WebUtility.HtmlEncode(blogName)}</strong> 的文章更新。</p>
                <p style="color:#64748b;">之后有新文章发布时，会通过这个邮箱通知你。</p>
            </div>
            """;

        var sent = await _emailService.SendEmailAsync(email, $"订阅成功 - {blogName}", body);
        if (!sent)
        {
            throw new Exception("订阅邮件发送失败，请检查邮箱设置");
        }
    }

    public async Task NotifyArticlePublishedAsync(ArticleDto article)
    {
        try
        {
            if (article.Id <= 0 || article.Status != 1)
            {
                return;
            }

            var settings = await _blogSettingsService.GetAsync();
            if (!settings.IsEmailNotificationOpen)
            {
                return;
            }

            var subscribers = await _dbContext.Db.Queryable<EmailSubscriber>()
                .Where(it => it.IsActive)
                .ToListAsync();

            if (subscribers.Count == 0)
            {
                _logger.LogInformation("No active email subscribers for article {ArticleId}", article.Id);
                return;
            }

            var blogName = string.IsNullOrWhiteSpace(settings.Name) ? "博客" : settings.Name;
            var subject = $"【{blogName}】新文章：{article.Title}";
            var htmlBody = BuildArticlePublishedHtml(article, blogName);
            var success = 0;

            foreach (var subscriber in subscribers)
            {
                if (string.IsNullOrWhiteSpace(subscriber.Email))
                {
                    continue;
                }

                if (await _emailService.SendEmailAsync(subscriber.Email, subject, htmlBody))
                {
                    success++;
                }
            }

            _logger.LogInformation(
                "Article publish notification sent for {ArticleId}. Success: {Success}/{Total}",
                article.Id,
                success,
                subscribers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send article publish notification for {ArticleId}", article.Id);
        }
    }

    private string BuildArticlePublishedHtml(ArticleDto article, string blogName)
    {
        var title = WebUtility.HtmlEncode(article.Title);
        var summary = WebUtility.HtmlEncode(GetArticleSummary(article));
        var articleUrl = BuildArticleUrl(article.Id);
        var category = WebUtility.HtmlEncode(article.CategoryName ?? "文章");
        var date = article.CreateTime == default ? DateTime.Now : article.CreateTime;

        return $"""
            <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;line-height:1.8;color:#1f2937;max-width:640px;margin:0 auto;">
                <p style="color:#64748b;margin:0 0 8px;">{WebUtility.HtmlEncode(blogName)} 发布了一篇新文章</p>
                <h2 style="margin:0 0 12px;color:#0f172a;">{title}</h2>
                <p style="margin:0 0 12px;color:#64748b;">{category} · {date:yyyy-MM-dd HH:mm}</p>
                <div style="background:#f8fafc;border:1px solid #e5e7eb;border-radius:12px;padding:16px;margin:18px 0;color:#334155;">
                    {summary}
                </div>
                <p style="margin:24px 0 0;">
                    <a href="{articleUrl}" style="display:inline-block;background:#2563eb;color:#fff;text-decoration:none;border-radius:8px;padding:10px 18px;font-weight:600;">阅读文章</a>
                </p>
            </div>
            """;
    }

    private string BuildArticleUrl(long articleId)
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]
            ?? _configuration["Site:BaseUrl"]
            ?? _configuration["App:PublicUrl"]
            ?? "https://www.qianjinge.com";

        return $"{baseUrl.TrimEnd('/')}/#/article/{articleId}";
    }

    private static string GetArticleSummary(ArticleDto article)
    {
        var source = !string.IsNullOrWhiteSpace(article.Summary) ? article.Summary : article.Content;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "有新文章发布了，点击按钮查看全文。";
        }

        var text = Regex.Replace(source, "<.*?>", " ");
        text = Regex.Replace(text, @"[`*_>#\[\]\(\)-]", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length > 180 ? text[..180] + "..." : text;
    }
}

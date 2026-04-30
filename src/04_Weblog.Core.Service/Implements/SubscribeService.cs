using System.Net.Mail;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class SubscribeService : ISubscribeService
{
    private readonly DbContext _dbContext;
    private readonly IBlogSettingsService _blogSettingsService;
    private readonly IEmailService _emailService;

    public SubscribeService(DbContext dbContext, IBlogSettingsService blogSettingsService, IEmailService emailService)
    {
        _dbContext = dbContext;
        _blogSettingsService = blogSettingsService;
        _emailService = emailService;
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
                <p>你已成功订阅 <strong>{blogName}</strong> 的文章更新。</p>
                <p style="color:#64748b;">之后有新文章时，会通过这个邮箱通知你。</p>
            </div>
            """;

        var sent = await _emailService.SendEmailAsync(email, $"订阅成功 - {blogName}", body);
        if (!sent)
        {
            throw new Exception("订阅邮件发送失败，请检查邮箱设置");
        }
    }
}

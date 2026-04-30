using Mapster;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class BlogSettingsService : IBlogSettingsService
{
    private readonly DbContext _dbContext;

    public BlogSettingsService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BlogSettingsDto> GetAsync()
    {
        var settings = await _dbContext.BlogSettingsDb.FirstAsync();
        if (settings == null)
        {
            settings = new BlogSettings
            {
                Logo = "",
                Name = "",
                Author = "",
                Introduction = "",
                Avatar = "",
                GithubHomepage = "",
                CsdnHomepage = "",
                GiteeHomepage = "",
                ZhihuHomepage = "",
                IsCommentSensiWordOpen = true,
                IsCommentExamineOpen = false,
                IsSubscribeCardOpen = true,
                SubscribeTitle = "订阅更新",
                SubscribeDescription = "订阅后，最新文章将通过邮件发送给你",
                SubscribePlaceholder = "输入你的邮箱地址",
                SubscribeButtonText = "订阅"
            };
            var id = await _dbContext.Db.Insertable(settings).ExecuteReturnIdentityAsync();
            settings.Id = id;
        }
        return settings.Adapt<BlogSettingsDto>();
    }

    public async Task<BlogSettingsDto> UpdateAsync(UpdateBlogSettingsRequest request)
    {
        var settings = await _dbContext.BlogSettingsDb.FirstAsync();
        if (settings == null)
        {
            settings = new BlogSettings
            {
                Logo = request.Logo,
                Name = request.Name,
                Author = request.Author,
                Introduction = request.Introduction,
                Avatar = request.Avatar,
                GithubHomepage = request.GithubHomepage,
                CsdnHomepage = request.CsdnHomepage,
                GiteeHomepage = request.GiteeHomepage,
                ZhihuHomepage = request.ZhihuHomepage,
                Mail = request.Mail,
                IsCommentSensiWordOpen = request.IsCommentSensiWordOpen,
                IsCommentExamineOpen = request.IsCommentExamineOpen,
                StickerZipMaxCount = request.StickerZipMaxCount,
                IsLinkPreviewOpen = request.IsLinkPreviewOpen,
                LinkPreviewWhitelist = request.LinkPreviewWhitelist,
                IsEmailNotificationOpen = request.IsEmailNotificationOpen,
                SmtpHost = request.SmtpHost,
                SmtpPort = request.SmtpPort,
                SmtpUsername = request.SmtpUsername,
                SmtpPassword = request.SmtpPassword,
                SmtpEnableSsl = request.SmtpEnableSsl,
                SmtpFromEmail = request.SmtpFromEmail,
                SmtpFromName = request.SmtpFromName,
                IsSubscribeCardOpen = request.IsSubscribeCardOpen,
                SubscribeTitle = request.SubscribeTitle,
                SubscribeDescription = request.SubscribeDescription,
                SubscribePlaceholder = request.SubscribePlaceholder,
                SubscribeButtonText = request.SubscribeButtonText
            };
            var id = await _dbContext.Db.Insertable(settings).ExecuteReturnIdentityAsync();
            settings.Id = id;
        }
        else
        {
            settings.Logo = request.Logo;
            settings.Name = request.Name;
            settings.Author = request.Author;
            settings.Introduction = request.Introduction;
            settings.Avatar = request.Avatar;
            settings.GithubHomepage = request.GithubHomepage;
            settings.CsdnHomepage = request.CsdnHomepage;
            settings.GiteeHomepage = request.GiteeHomepage;
            settings.ZhihuHomepage = request.ZhihuHomepage;
            settings.Mail = request.Mail;
            settings.IsCommentSensiWordOpen = request.IsCommentSensiWordOpen;
            settings.IsCommentExamineOpen = request.IsCommentExamineOpen;
            settings.StickerZipMaxCount = request.StickerZipMaxCount;
            settings.IsLinkPreviewOpen = request.IsLinkPreviewOpen;
            settings.LinkPreviewWhitelist = request.LinkPreviewWhitelist;
            settings.IsEmailNotificationOpen = request.IsEmailNotificationOpen;
            settings.SmtpHost = request.SmtpHost;
            settings.SmtpPort = request.SmtpPort;
            settings.SmtpUsername = request.SmtpUsername;
            settings.SmtpPassword = request.SmtpPassword;
            settings.SmtpEnableSsl = request.SmtpEnableSsl;
            settings.SmtpFromEmail = request.SmtpFromEmail;
            settings.SmtpFromName = request.SmtpFromName;
            settings.IsSubscribeCardOpen = request.IsSubscribeCardOpen;
            settings.SubscribeTitle = request.SubscribeTitle;
            settings.SubscribeDescription = request.SubscribeDescription;
            settings.SubscribePlaceholder = request.SubscribePlaceholder;
            settings.SubscribeButtonText = request.SubscribeButtonText;
            await _dbContext.Db.Updateable(settings).ExecuteCommandAsync();
        }
        return settings.Adapt<BlogSettingsDto>();
    }

    public async Task<string> UploadImageAsync(string folder, string fileName, byte[] fileData)
    {
        // Return a placeholder path - actual upload handled by FileController with MinIO
        var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        return $"/uploads/{folder}/{newFileName}";
    }
}

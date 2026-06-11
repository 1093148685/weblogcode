using Weblog.Core.Model.Entities;

namespace Weblog.Core.Service.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
    Task<bool> SendCommentNotificationToAdminAsync(Comment comment, string? replyToNickname = null);
    Task<bool> SendReplyNotificationToUserAsync(Comment comment, Comment parentComment);
}

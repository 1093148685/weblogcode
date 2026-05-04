using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface ISubscribeService
{
    Task SubscribeAsync(string email, string? ipAddress);
    Task NotifyArticlePublishedAsync(ArticleDto article);
}

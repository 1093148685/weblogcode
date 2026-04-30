namespace Weblog.Core.Service.Interfaces;

public interface ISubscribeService
{
    Task SubscribeAsync(string email, string? ipAddress);
}

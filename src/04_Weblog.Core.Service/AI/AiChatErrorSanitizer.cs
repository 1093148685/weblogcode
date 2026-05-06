using System.Net;

namespace Weblog.Core.Service.AI;

public static class AiChatErrorSanitizer
{
    private const string FrequentMessage = "请求过于频繁，请稍后再试";
    private const string BusyMessage = "服务暂时不可用，请稍后再试";

    public static string ToUserMessage(Exception ex)
    {
        return ToUserMessage(ex.Message);
    }

    public static string ToUserMessage(string? message)
    {
        var raw = (message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return BusyMessage;

        var lower = raw.ToLowerInvariant();
        if (IsFrequentOrUnauthorized(lower))
            return FrequentMessage;

        return raw;
    }

    private static bool IsFrequentOrUnauthorized(string message)
    {
        return message.Contains(((int)HttpStatusCode.Unauthorized).ToString(), StringComparison.Ordinal)
            && (message.Contains("unauthorized", StringComparison.Ordinal)
                || message.Contains("authorization required", StringComparison.Ordinal))
            || message.Contains(((int)HttpStatusCode.TooManyRequests).ToString(), StringComparison.Ordinal)
            || message.Contains("too many requests", StringComparison.Ordinal)
            || message.Contains("rate limit", StringComparison.Ordinal)
            || message.Contains("请求频繁", StringComparison.Ordinal)
            || message.Contains("过于频繁", StringComparison.Ordinal)
            || message.Contains("频繁", StringComparison.Ordinal);
    }
}

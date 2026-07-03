using System.Collections.Concurrent;

namespace Weblog.Core.Common.Utils;

public class CaptchaStore
{
    private static readonly ConcurrentDictionary<string, CaptchaInfo> _captchas = new();
    private static readonly ConcurrentDictionary<string, FailedAttempt> _failedAttempts = new();

    public class CaptchaInfo
    {
        public string Answer { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }

    public class FailedAttempt
    {
        public int Count { get; set; }
        public DateTime? WaitUntil { get; set; }
    }

    public static string Generate(string question)
    {
        var id = Guid.NewGuid().ToString("N");
        var captcha = new CaptchaInfo
        {
            Answer = question,
            ExpiresAt = DateTime.Now.AddMinutes(5),
            IsUsed = false
        };
        _captchas[id] = captcha;

        var expiredKeys = _captchas.Where(kvp => kvp.Value.ExpiresAt < DateTime.Now).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
            _captchas.TryRemove(key, out _);

        return id;
    }

    public static CaptchaInfo? Get(string id)
    {
        if (_captchas.TryGetValue(id, out var captcha))
        {
            if (captcha.ExpiresAt < DateTime.Now)
            {
                _captchas.TryRemove(id, out _);
                return null;
            }
            return captcha;
        }
        return null;
    }

    public static bool ValidateAndMark(string id, string answer)
    {
        var captcha = Get(id);
        if (captcha == null || captcha.IsUsed)
            return false;

        if (captcha.Answer != answer)
            return false;

        captcha.IsUsed = true;
        return true;
    }

    public static void RecordFailedAttempt(string key)
    {
        if (_failedAttempts.TryGetValue(key, out var attempt))
        {
            attempt.Count++;
            if (attempt.Count >= 3)
                attempt.WaitUntil = DateTime.Now.AddSeconds(60);
        }
        else
        {
            _failedAttempts[key] = new FailedAttempt { Count = 1 };
        }
    }

    public static void ClearFailedAttempt(string key)
    {
        _failedAttempts.TryRemove(key, out _);
    }

    public static (bool canTry, int waitSeconds) CanTry(string key)
    {
        if (_failedAttempts.TryGetValue(key, out var attempt))
        {
            if (attempt.WaitUntil.HasValue && attempt.WaitUntil.Value > DateTime.Now)
            {
                return (false, (int)(attempt.WaitUntil.Value - DateTime.Now).TotalSeconds);
            }
            if (attempt.Count >= 3)
            {
                return (false, 60);
            }
        }
        return (true, 0);
    }
}

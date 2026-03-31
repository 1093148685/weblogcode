using System.Collections.Concurrent;

namespace Weblog.Core.Api.Services;

public class InMemoryCacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    public T? Get<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var item))
        {
            if (item.ExpiresAt > DateTime.UtcNow)
            {
                return item.Value as T;
            }
            _cache.TryRemove(key, out _);
        }
        return null;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var expiresAt = expiration.HasValue 
            ? DateTime.UtcNow.Add(expiration.Value) 
            : DateTime.UtcNow.AddHours(24);

        _cache[key] = new CacheItem
        {
            Value = value,
            ExpiresAt = expiresAt
        };
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public bool Exists(string key)
    {
        if (_cache.TryGetValue(key, out var item))
        {
            if (item.ExpiresAt > DateTime.UtcNow)
            {
                return true;
            }
            _cache.TryRemove(key, out _);
        }
        return false;
    }

    private class CacheItem
    {
        public object Value { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}

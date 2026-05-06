using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI.Core;

public class AiProviderSelector
{
    private readonly ProviderRegistry _registry;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly ILogger<AiProviderSelector> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, List<ApiKeyState>> _keyPools = new();
    private readonly List<AiProviderConfig> _providerConfigs = new();
    private readonly object _lock = new();

    public AiProviderSelector(ProviderRegistry registry, IAiKeyEncryptionService encryption, ILogger<AiProviderSelector> logger, IServiceProvider serviceProvider)
    {
        _registry = registry;
        _encryption = encryption;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void InitializeKeyPools(IEnumerable<AiProviderConfig> providers)
    {
        lock (_lock)
        {
            _keyPools.Clear();
            _providerConfigs.Clear();
            _providerConfigs.AddRange(providers.Select(provider =>
            {
                var advanced = AiProviderConfigParser.Parse(provider.Config);
                provider.Protocol = advanced.Protocol;
                provider.Prefix = advanced.Prefix;
                return provider;
            }));
            
            foreach (var provider in _providerConfigs.Where(p => p.IsEnabled))
            {
                if (!_keyPools.ContainsKey(provider.Name.ToLower()))
                {
                    _keyPools[provider.Name.ToLower()] = new List<ApiKeyState>();
                }
                
                var decryptedKeys = GetDecryptedKeys(provider);

                if (decryptedKeys.Count == 0)
                {
                    _logger.LogWarning("Using legacy key for provider {Name}", provider.Name);
                    
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<Weblog.Core.Repository.DbContext>();

                        var legacyModel = dbContext.Db.Queryable<AiModel>()
                            .Where(m => m.IsEnabled == true)
                            .ToList()
                            .FirstOrDefault(m => IsLegacyModelForProvider(m, provider));

                        if (legacyModel != null && !string.IsNullOrEmpty(legacyModel.ApiKey))
                        {
                            decryptedKeys.Add(legacyModel.ApiKey);
                            _logger.LogInformation("Got legacy key for {Name}, length={Len}", provider.Name, legacyModel.ApiKey.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get legacy key for {Name}", provider.Name);
                    }
                }
                
                if (decryptedKeys.Count == 0)
                {
                    _logger.LogWarning("Provider {Name} has no valid key, SKIPPING", provider.Name);
                    continue;
                }

                provider.ApiKey = decryptedKeys.FirstOrDefault();

                foreach (var key in decryptedKeys)
                {
                    // Skip test/fake keys
                    if (key.Trim().ToLower().StartsWith("sk-test") || key.Trim().Length < 20)
                    {
                        _logger.LogWarning("Skipping test/fake key for {Provider}", provider.Name);
                        continue;
                    }
                    
                    _keyPools[provider.Name.ToLower()].Add(new ApiKeyState
                    {
                        Key = key.Trim(),
                        FailCount = 0,
                        LastUsed = DateTime.MinValue,
                        IsHealthy = true
                    });
                }
                
                _logger.LogInformation("Provider {Name} initialized with {Count} keys", provider.Name, decryptedKeys.Count);
            }
        }
    }

    public async Task<(IAiProvider? provider, string? apiKey, string? error)> SelectAsync(string? preferredProvider = null, AiProviderType type = AiProviderType.Chat)
    {
        var providers = GetEnabledProviders(type)
            .OrderBy(p => p.Priority)
            .ToList();

        _logger.LogInformation("SelectAsync: Found {Count} enabled providers", providers.Count);
        
        if (preferredProvider != null && providers.Any(p => MatchesProvider(p, preferredProvider)))
        {
            providers = providers.Where(p => MatchesProvider(p, preferredProvider))
                .Concat(providers.Where(p => !MatchesProvider(p, preferredProvider)))
                .ToList();
        }

        foreach (var config in providers)
        {
            _logger.LogInformation("Checking provider {Name}...", config.Name);
            
            var provider = _registry.GetForConfig(config);

            var key = SelectKey(config.Name);
            if (key == null) 
            {
                _logger.LogWarning("No key available for provider {Name}", config.Name);
                continue;
            }

            try
            {
                _logger.LogInformation("Trying provider {Name} with key prefix {KeyPrefix}", config.Name, key.Substring(0, Math.Min(8, key.Length)));
                RecordSuccess(config.Name, key);
                _logger.LogInformation("Provider {Name} selected successfully", config.Name);
                return (provider, key, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed: {Error}", config.Name, ex.Message);
                RecordFailure(config.Name, key);
                _logger.LogInformation("Trying next provider...");
                continue;
            }
        }

        _logger.LogWarning("No provider available after trying all {Count} providers", providers.Count);
        return (null, null, "No available AI provider");
    }

    public async IAsyncEnumerable<(IAiProvider? provider, string? apiKey, string? error)> SelectWithFallbackAsync(
        string? preferredProvider = null,
        AiProviderType type = AiProviderType.Chat,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var providers = GetEnabledProviders(type)
            .OrderBy(p => p.Priority)
            .ToList();

        if (preferredProvider != null && providers.Any(p => MatchesProvider(p, preferredProvider)))
        {
            providers = providers.Where(p => MatchesProvider(p, preferredProvider))
                .Concat(providers.Where(p => !MatchesProvider(p, preferredProvider)))
                .ToList();
        }

        foreach (var config in providers)
        {
            var provider = _registry.GetForConfig(config);

            var key = SelectKey(config.Name);
            if (key == null) continue;

            yield return (provider, key, null);

            var allKeysFailed = _keyPools.TryGetValue(config.Name.ToLower(), out var keys) 
                && keys?.All(k => !k.IsHealthy) == true;
            
            if (!allKeysFailed)
                break;
        }
    }

    private List<AiProviderConfig> GetEnabledProviders(AiProviderType type)
    {
        lock (_lock)
        {
            // Embedding 请求：优先匹配 Embedding 类型，没有则 fallback 到 Chat 类型
            // （OpenAI / DeepSeek 等 Chat Provider 同时支持 Embedding API，用同一个 Key）
            if (type == AiProviderType.Embedding)
            {
                var embeddingProviders = _providerConfigs
                    .Where(p => p.IsEnabled && p.Type == AiProviderType.Embedding)
                    .ToList();
                if (embeddingProviders.Count > 0)
                    return embeddingProviders;

                // fallback：Chat Provider 也支持 Embedding
                return _providerConfigs
                    .Where(p => p.IsEnabled && p.Type == AiProviderType.Chat)
                    .ToList();
            }

            return _providerConfigs
                .Where(p => p.IsEnabled && (type == AiProviderType.Chat || p.Type == type))
                .ToList();
        }
    }

    private List<string> GetDecryptedKeys(AiProviderConfig provider)
    {
        var result = new List<string>();
        var advanced = AiProviderConfigParser.Parse(provider.Config);

        foreach (var keyEntry in advanced.Keys.Where(k => k.IsEnabled && !string.IsNullOrWhiteSpace(k.Value)))
        {
            var decrypted = DecryptKeyValue(provider.Name, keyEntry.Value);
            if (!string.IsNullOrWhiteSpace(decrypted))
                result.Add(decrypted);
        }

        if (result.Count > 0)
            return result.Distinct(StringComparer.Ordinal).ToList();

        var legacyKey = DecryptKeyValue(provider.Name, provider.EncryptedApiKey);
        if (!string.IsNullOrWhiteSpace(legacyKey))
        {
            result.AddRange(legacyKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return result.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal).ToList();
    }

    private string DecryptKeyValue(string providerName, string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
            return "";

        try
        {
            var decrypted = _encryption.Decrypt(encryptedValue);
            return string.IsNullOrWhiteSpace(decrypted) ? encryptedValue : decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt key for provider {Name}", providerName);
            return encryptedValue;
        }
    }

    private static bool MatchesProvider(AiProviderConfig config, string providerHint)
    {
        if (string.IsNullOrWhiteSpace(providerHint))
            return false;

        return config.Name.Equals(providerHint, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(config.Prefix) && config.Prefix.Equals(providerHint, StringComparison.OrdinalIgnoreCase))
            || config.DisplayName.Equals(providerHint, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLegacyModelForProvider(AiModel model, AiProviderConfig provider)
    {
        var providerIds = new[]
            {
                provider.Name,
                provider.Prefix,
                provider.DisplayName
            }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (providerIds.Count == 0)
            return false;

        var legacyType = model.Type?.Trim();
        if (!string.IsNullOrWhiteSpace(legacyType) && providerIds.Contains(legacyType))
            return true;

        var modelRoute = AiProviderConfigParser.ParseModelRoute(model.Model);
        return !string.IsNullOrWhiteSpace(modelRoute.ProviderHint)
            && providerIds.Contains(modelRoute.ProviderHint);
    }

    private string? SelectKey(string providerName)
    {
        lock (_lock)
        {
            if (!_keyPools.TryGetValue(providerName.ToLower(), out var keys) || keys.Count == 0)
                return null;

            var healthyKeys = keys.Where(k => k.IsHealthy && k.FailCount < 3).OrderBy(k => k.LastUsed).ToList();
            if (healthyKeys.Count == 0)
                return keys.OrderBy(k => k.FailCount).First().Key;

            var selected = healthyKeys.First();
            selected.LastUsed = DateTime.Now;
            return selected.Key;
        }
    }

    public void RecordSuccess(string providerName, string key)
    {
        lock (_lock)
        {
            if (_keyPools.TryGetValue(providerName.ToLower(), out var keys))
            {
                var keyState = keys.FirstOrDefault(k => k.Key == key);
                if (keyState != null)
                {
                    keyState.FailCount = 0;
                    keyState.IsHealthy = true;
                }
            }
        }
    }

    public void RecordFailure(string providerName, string key)
    {
        lock (_lock)
        {
            if (_keyPools.TryGetValue(providerName.ToLower(), out var keys))
            {
                var keyState = keys.FirstOrDefault(k => k.Key == key);
                if (keyState != null)
                {
                    keyState.FailCount++;
                    if (keyState.FailCount >= 3)
                    {
                        keyState.IsHealthy = false;
                        _logger.LogWarning("API key for {Provider} marked as unhealthy after {FailCount} failures", providerName, keyState.FailCount);
                    }
                }
            }
        }
    }

    public void ResetKeyHealth(string providerName, string? key = null)
    {
        lock (_lock)
        {
            if (_keyPools.TryGetValue(providerName.ToLower(), out var keys))
            {
                if (key == null)
                {
                    foreach (var k in keys)
                    {
                        k.FailCount = 0;
                        k.IsHealthy = true;
                    }
                }
                else
                {
                    var keyState = keys.FirstOrDefault(k => k.Key == key);
                    if (keyState != null)
                    {
                        keyState.FailCount = 0;
                        keyState.IsHealthy = true;
                    }
                }
            }
        }
    }

    /// <summary>获取指定 Provider 的 ApiUrl（供 Embedding 等直接构造 URL 使用）</summary>
    public string? GetApiUrl(string providerName)
    {
        lock (_lock)
        {
            var config = _providerConfigs.FirstOrDefault(p =>
                MatchesProvider(p, providerName));
            return string.IsNullOrWhiteSpace(config?.ApiUrl) ? null : config!.ApiUrl.TrimEnd('/');
        }
    }

    /// <summary>获取所有 Provider 的 Key Pool 健康状态（脱敏）</summary>
    public List<ProviderKeyPoolStatus> GetKeyPoolStatus()
    {
        lock (_lock)
        {
            var result = new List<ProviderKeyPoolStatus>();
            foreach (var kvp in _keyPools)
            {
                var providerName = kvp.Key;
                var keys = kvp.Value;
                result.Add(new ProviderKeyPoolStatus
                {
                    ProviderName = providerName,
                    TotalKeys    = keys.Count,
                    HealthyKeys  = keys.Count(k => k.IsHealthy),
                    Keys = keys.Select(k => new KeyStatus
                    {
                        KeyPrefix  = k.Key.Length >= 8 ? k.Key[..4] + "****" + k.Key[^4..] : "****",
                        IsHealthy  = k.IsHealthy,
                        FailCount  = k.FailCount,
                        LastUsed   = k.LastUsed == DateTime.MinValue ? null : k.LastUsed
                    }).ToList()
                });
            }
            return result;
        }
    }

    /// <summary>获取所有已启用 Provider 的配置（用于健康检查）</summary>
    public List<AiProviderConfig> GetEnabledProviderConfigs()
    {
        lock (_lock) { return _providerConfigs.Where(p => p.IsEnabled).ToList(); }
    }
}

public class ApiKeyState
{
    public string Key { get; set; } = string.Empty;
    public int FailCount { get; set; }
    public DateTime LastUsed { get; set; }
    public bool IsHealthy { get; set; } = true;
}

public class ProviderKeyPoolStatus
{
    public string ProviderName { get; set; } = string.Empty;
    public int TotalKeys { get; set; }
    public int HealthyKeys { get; set; }
    public List<KeyStatus> Keys { get; set; } = new();
}

public class KeyStatus
{
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public int FailCount { get; set; }
    public DateTime? LastUsed { get; set; }
}

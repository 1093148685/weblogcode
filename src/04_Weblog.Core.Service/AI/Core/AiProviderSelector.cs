using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
            _providerConfigs.AddRange(providers);
            
            foreach (var provider in providers.Where(p => p.IsEnabled))
            {
                if (!_keyPools.ContainsKey(provider.Name.ToLower()))
                {
                    _keyPools[provider.Name.ToLower()] = new List<ApiKeyState>();
                }
                
                string? decryptedKey = null;
                
                // First try DPAPI decryption
                try
                {
                    decryptedKey = _encryption.Decrypt(provider.EncryptedApiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DPAPI decryption failed for {Name}", provider.Name);
                }
                
                // If decryption failed, empty, or returned same as input, use legacy key
                if (string.IsNullOrWhiteSpace(decryptedKey) || decryptedKey == provider.EncryptedApiKey)
                {
                    _logger.LogWarning("Using legacy key for provider {Name}", provider.Name);
                    
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<Weblog.Core.Repository.DbContext>();
                        
                        var legacyModel = dbContext.Db.Queryable<Weblog.Core.Model.Entities.AiModel>()
                            .Where(m => m.IsEnabled == true)
                            .First();
                        
                        if (legacyModel != null && !string.IsNullOrEmpty(legacyModel.ApiKey))
                        {
                            decryptedKey = legacyModel.ApiKey;
                            _logger.LogInformation("Got legacy key for {Name}, length={Len}", provider.Name, legacyModel.ApiKey.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get legacy key for {Name}", provider.Name);
                    }
                }
                
                if (string.IsNullOrWhiteSpace(decryptedKey))
                {
                    _logger.LogWarning("Provider {Name} has no valid key, SKIPPING", provider.Name);
                    continue;
                }
                
                var keys = decryptedKey.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var key in keys)
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
                
                _logger.LogInformation("Provider {Name} initialized with {Count} keys", provider.Name, keys.Length);
            }
        }
    }

    public async Task<(IAiProvider? provider, string? apiKey, string? error)> SelectAsync(string? preferredProvider = null, AiProviderType type = AiProviderType.Chat)
    {
        var providers = GetEnabledProviders(type)
            .OrderBy(p => p.Priority)
            .ToList();

        _logger.LogInformation("SelectAsync: Found {Count} enabled providers", providers.Count);
        
        if (preferredProvider != null && providers.Any(p => p.Name.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase)))
        {
            providers = providers.Where(p => p.Name.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase))
                .Concat(providers.Where(p => !p.Name.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        foreach (var config in providers)
        {
            _logger.LogInformation("Checking provider {Name}...", config.Name);
            
            var provider = _registry.Get(config.Name);
            if (provider == null) 
            {
                _logger.LogWarning("Provider {Name} not found in registry", config.Name);
                continue;
            }

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

        foreach (var config in providers)
        {
            var provider = _registry.Get(config.Name);
            if (provider == null) continue;

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
            return _providerConfigs
                .Where(p => p.IsEnabled && (type == AiProviderType.Chat || p.Type == type))
                .ToList();
        }
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
}

public class ApiKeyState
{
    public string Key { get; set; } = string.Empty;
    public int FailCount { get; set; }
    public DateTime LastUsed { get; set; }
    public bool IsHealthy { get; set; } = true;
}
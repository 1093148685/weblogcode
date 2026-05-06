using Microsoft.Extensions.Logging;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI;

public interface IAiProviderService
{
    Task<List<AiProviderDto>> GetAllProvidersAsync();
    Task<AiProviderDto?> GetProviderByIdAsync(long id);
    Task<AiProviderDto> CreateProviderAsync(CreateAiProviderRequest request);
    Task<AiProviderDto> UpdateProviderAsync(long id, UpdateAiProviderRequest request);
    Task<bool> DeleteProviderAsync(long id);
    Task<bool> TestConnectionAsync(long id);
    Task<bool> TestConnectionAsync(long id, TestAiProviderRequest? request);
    Task<List<AiModelOptionDto>> FetchModelsAsync(FetchModelsRequest request);
    Task<List<AiModelOptionDto>> FetchProviderModelsAsync(long id, FetchModelsRequest? request = null);
    Task MigrateLegacyDataAsync();
    Task InitializeKeyPoolsAsync();
}

public class AiProviderService : IAiProviderService
{
    private readonly DbContext _dbContext;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly ProviderRegistry _registry;
    private readonly AiProviderSelector _selector;
    private readonly ILogger<AiProviderService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public AiProviderService(
        DbContext dbContext,
        IAiKeyEncryptionService encryption,
        ProviderRegistry registry,
        AiProviderSelector selector,
        ILogger<AiProviderService> logger,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _encryption = encryption;
        _registry = registry;
        _selector = selector;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<List<AiProviderDto>> GetAllProvidersAsync()
    {
        var providers = await _dbContext.AiProviderDb.OrderBy(p => p.Priority).ToListAsync();
        return providers.Select(ToDto).ToList();
    }

    public async Task<AiProviderDto?> GetProviderByIdAsync(long id)
    {
        var provider = await _dbContext.AiProviderDb.Where(p => p.Id == id).FirstAsync();
        if (provider == null) return null;

        return ToDto(provider);
    }

    public async Task<AiProviderDto> CreateProviderAsync(CreateAiProviderRequest request)
    {
        ValidateCreateRequest(request);
        var (storedConfig, encryptedLegacyKeys) = BuildStoredConfig(request.ConfigData, request.Config, request.Protocol, request.Prefix, request.ApiKey, null);

        var provider = new AiProvider
        {
            Name = NormalizeProviderName(request.Name),
            DisplayName = request.DisplayName.Trim(),
            Type = NormalizeProviderType(request.Type),
            ApiUrl = NormalizeApiUrl(request.ApiUrl),
            EncryptedApiKey = encryptedLegacyKeys,
            IsEnabled = request.IsEnabled,
            Priority = request.Priority,
            Config = storedConfig
        };

        provider.Id = await _dbContext.Db.Insertable(provider).ExecuteReturnIdentityAsync();
        
        await InitializeKeyPoolsAsync();

        return ToDto(provider);
    }

    public async Task<AiProviderDto> UpdateProviderAsync(long id, UpdateAiProviderRequest request)
    {
        ValidateUpdateRequest(request);

        var provider = await _dbContext.AiProviderDb.Where(p => p.Id == id).FirstAsync();
        if (provider == null)
            throw new Exception("Provider not found");

        var (storedConfig, encryptedLegacyKeys) = BuildStoredConfig(request.ConfigData, request.Config, request.Protocol, request.Prefix, request.ApiKey, provider);

        provider.DisplayName = request.DisplayName.Trim();
        provider.Type = NormalizeProviderType(request.Type);
        provider.ApiUrl = NormalizeApiUrl(request.ApiUrl);
        provider.EncryptedApiKey = encryptedLegacyKeys;
        provider.IsEnabled = request.IsEnabled;
        provider.Priority = request.Priority;
        provider.Config = storedConfig;
        provider.UpdatedAt = DateTime.Now;

        await _dbContext.Db.Updateable(provider).ExecuteCommandAsync();
        
        await InitializeKeyPoolsAsync();

        return ToDto(provider);
    }

    public async Task<bool> DeleteProviderAsync(long id)
    {
        var result = await _dbContext.Db.Deleteable<AiProvider>().Where(p => p.Id == id).ExecuteCommandAsync();
        await InitializeKeyPoolsAsync();
        return result > 0;
    }

    public async Task<bool> TestConnectionAsync(long id)
    {
        return await TestConnectionAsync(id, null);
    }

    public async Task<bool> TestConnectionAsync(long id, TestAiProviderRequest? request)
    {
        var provider = await _dbContext.AiProviderDb.Where(p => p.Id == id).FirstAsync();
        if (provider == null)
            return false;

        var config = BuildRuntimeConfig(provider);
        if (!string.IsNullOrWhiteSpace(request?.Model))
        {
            var route = AiProviderConfigParser.ParseModelRoute(request.Model);
            var advanced = AiProviderConfigParser.Parse(config.Config);
            if (!advanced.Models.Any(m => m.Id.Equals(route.ModelId, StringComparison.OrdinalIgnoreCase)))
            {
                advanced.Models.Insert(0, new AiProviderModelConfigDto
                {
                    Id = route.ModelId,
                    Name = route.ModelId,
                    IsEnabled = true,
                    IsDefault = true
                });
                config.Config = AiProviderConfigParser.Serialize(advanced);
            }
        }

        var aiProvider = _registry.GetForConfig(config);

        var apiKey = GetFirstEnabledKey(provider, request?.KeyId);
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("TestConnection: provider {Name} has no enabled API key", provider.Name);
            return false;
        }
        
        return await aiProvider.TestConnectionAsync(provider.ApiUrl, apiKey);
    }

    public async Task<List<AiModelOptionDto>> FetchProviderModelsAsync(long id, FetchModelsRequest? request = null)
    {
        var provider = await _dbContext.AiProviderDb.Where(p => p.Id == id).FirstAsync();
        if (provider == null)
            throw new Exception("Provider not found");

        var advanced = AiProviderConfigParser.Parse(provider.Config);
        var fetchRequest = new FetchModelsRequest
        {
            ApiUrl = string.IsNullOrWhiteSpace(request?.ApiUrl) ? provider.ApiUrl : request!.ApiUrl,
            ApiKey = string.IsNullOrWhiteSpace(request?.ApiKey) ? GetFirstEnabledKey(provider) : request!.ApiKey,
            ModelsPath = string.IsNullOrWhiteSpace(request?.ModelsPath) ? advanced.ModelsPath : request!.ModelsPath,
            Headers = request?.Headers?.Count > 0 ? request.Headers : advanced.Headers
        };

        return await FetchModelsAsync(fetchRequest);
    }

    public async Task<List<AiModelOptionDto>> FetchModelsAsync(FetchModelsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiUrl))
            throw new Exception("API URL 不能为空");

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        ApplyRequestHeaders(httpClient, request.Headers, request.ApiKey);

        var modelsPath = string.IsNullOrWhiteSpace(request.ModelsPath) ? "/models" : request.ModelsPath;
        if (!modelsPath.StartsWith('/'))
            modelsPath = "/" + modelsPath;

        var modelsUrl = $"{request.ApiUrl.TrimEnd('/')}{modelsPath}";
        var response = await httpClient.GetAsync(modelsUrl);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"请求失败: {response.StatusCode}\n{content}");

        return AiProviderConfigParser.ParseModelResponse(content);
    }

    public async Task MigrateLegacyDataAsync()
    {
        var logger = _loggerFactory.CreateLogger<Legacy.AiMigrationService>();
        var migrationService = new Legacy.AiMigrationService(_dbContext, _encryption, logger);
        await migrationService.MigrateIfNeededAsync();
    }

    public async Task InitializeKeyPoolsAsync()
    {
        var providers = await _dbContext.AiProviderDb.ToListAsync();
        var configs = providers.Select(p =>
        {
            var advanced = AiProviderConfigParser.Parse(p.Config);
            return new AiProviderConfig
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Type = Enum.TryParse<AiProviderType>(p.Type, true, out var type) ? type : AiProviderType.Chat,
                Protocol = advanced.Protocol,
                Prefix = advanced.Prefix,
                ApiUrl = p.ApiUrl,
                EncryptedApiKey = p.EncryptedApiKey,
                IsEnabled = p.IsEnabled,
                Priority = p.Priority,
                Config = p.Config
            };
        }).ToList();

        _selector.InitializeKeyPools(configs);
    }

    private AiProviderDto ToDto(AiProvider provider)
    {
        var config = AiProviderConfigParser.Parse(provider.Config);
        if (!HasExplicitProtocol(provider.Config))
            config.Protocol = InferProtocol(provider.Name, config.Protocol);
        var responseConfig = CloneConfig(config);

        foreach (var key in responseConfig.Keys)
        {
            var plainKey = SafeDecrypt(key.Value);
            key.MaskedValue = AiProviderConfigParser.MaskKey(plainKey);
            key.Value = "";
        }

        return new AiProviderDto
        {
            Id = provider.Id,
            Name = provider.Name,
            DisplayName = provider.DisplayName,
            Type = provider.Type,
            Protocol = config.Protocol,
            Prefix = config.Prefix,
            ApiUrl = provider.ApiUrl,
            ApiKey = "",
            IsEnabled = provider.IsEnabled,
            Priority = provider.Priority,
            Config = provider.Config,
            ModelCount = config.Models.Count(m => m.IsEnabled),
            KeyCount = config.Keys.Count(k => k.IsEnabled),
            ConfigData = responseConfig,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt
        };
    }

    private static void ValidateCreateRequest(CreateAiProviderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Provider 名称不能为空");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new Exception("Provider 显示名称不能为空");
        if (false)
            throw new Exception("API Key 不能为空");
    }

    private static void ValidateUpdateRequest(UpdateAiProviderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new Exception("Provider 显示名称不能为空");
    }

    private static string NormalizeProviderName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    private static string NormalizeProviderType(string? type)
    {
        return string.IsNullOrWhiteSpace(type) ? AiProviderType.Chat.ToString().ToLowerInvariant() : type.Trim().ToLowerInvariant();
    }

    private static string NormalizeApiUrl(string? apiUrl)
    {
        return apiUrl?.Trim().TrimEnd('/') ?? "";
    }

    private static bool HasExplicitProtocol(string? config)
    {
        return !string.IsNullOrWhiteSpace(config)
            && config.Contains("\"protocol\"", StringComparison.OrdinalIgnoreCase);
    }

    private static string InferProtocol(string providerName, string fallback)
    {
        var name = providerName.ToLowerInvariant();
        if (name.Contains("claude") || name.Contains("anthropic")) return "anthropic";
        if (name.Contains("gemini") || name.Contains("google")) return "gemini";
        if (name.Contains("azure")) return "azure";
        return string.IsNullOrWhiteSpace(fallback) ? "openai-compatible" : fallback;
    }

    private (string Config, string EncryptedLegacyKeys) BuildStoredConfig(
        AiProviderAdvancedConfigDto? incomingConfig,
        string? rawConfig,
        string? protocol,
        string? prefix,
        string? legacyApiKey,
        AiProvider? existingProvider)
    {
        var config = incomingConfig ?? (!string.IsNullOrWhiteSpace(rawConfig)
            ? AiProviderConfigParser.Parse(rawConfig)
            : existingProvider == null
                ? new AiProviderAdvancedConfigDto()
                : AiProviderConfigParser.Parse(existingProvider.Config));

        if (!string.IsNullOrWhiteSpace(protocol))
            config.Protocol = protocol;
        else if (existingProvider != null && !HasExplicitProtocol(rawConfig ?? existingProvider.Config))
            config.Protocol = InferProtocol(existingProvider.Name, config.Protocol);
        if (prefix != null)
            config.Prefix = prefix;

        var existingConfig = existingProvider == null
            ? new AiProviderAdvancedConfigDto()
            : AiProviderConfigParser.Parse(existingProvider.Config);
        var existingKeys = existingConfig.Keys
            .Where(k => !string.IsNullOrWhiteSpace(k.Id))
            .ToDictionary(k => k.Id, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(legacyApiKey))
        {
            config.Keys.Add(new AiProviderKeyConfigDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Value = legacyApiKey.Trim(),
                IsEnabled = true
            });
        }

        var storedKeys = new List<AiProviderKeyConfigDto>();
        var enabledPlainKeys = new List<string>();

        foreach (var key in config.Keys)
        {
            var keyId = string.IsNullOrWhiteSpace(key.Id) ? Guid.NewGuid().ToString("N") : key.Id;
            var inputValue = key.Value?.Trim() ?? "";
            string encryptedValue;
            string plainValue;

            if (string.IsNullOrWhiteSpace(inputValue) || inputValue.Contains("****", StringComparison.Ordinal))
            {
                if (!existingKeys.TryGetValue(keyId, out var existingKey) || string.IsNullOrWhiteSpace(existingKey.Value))
                    continue;

                encryptedValue = existingKey.Value;
                plainValue = SafeDecrypt(existingKey.Value);
            }
            else if (existingKeys.TryGetValue(keyId, out var existingKey) && existingKey.Value == inputValue)
            {
                encryptedValue = existingKey.Value;
                plainValue = SafeDecrypt(existingKey.Value);
            }
            else
            {
                plainValue = inputValue;
                encryptedValue = _encryption.Encrypt(plainValue);
            }

            if (key.IsEnabled && !string.IsNullOrWhiteSpace(plainValue))
                enabledPlainKeys.Add(plainValue);

            storedKeys.Add(new AiProviderKeyConfigDto
            {
                Id = keyId,
                Value = encryptedValue,
                ProxyUrl = key.ProxyUrl,
                IsEnabled = key.IsEnabled,
                Status = string.IsNullOrWhiteSpace(key.Status) ? "unknown" : key.Status,
                LastTestedAt = key.LastTestedAt
            });
        }

        config.Keys = storedKeys;

        var serializedConfig = AiProviderConfigParser.Serialize(config);
        var legacyKeys = enabledPlainKeys.Count == 0 ? "" : _encryption.Encrypt(string.Join(",", enabledPlainKeys));
        return (serializedConfig, legacyKeys);
    }

    private AiProviderConfig BuildRuntimeConfig(AiProvider provider)
    {
        var advanced = AiProviderConfigParser.Parse(provider.Config);
        return new AiProviderConfig
        {
            Id = provider.Id,
            Name = provider.Name,
            DisplayName = provider.DisplayName,
            Type = Enum.TryParse<AiProviderType>(provider.Type, true, out var type) ? type : AiProviderType.Chat,
            Protocol = advanced.Protocol,
            Prefix = advanced.Prefix,
            ApiUrl = provider.ApiUrl,
            EncryptedApiKey = provider.EncryptedApiKey,
            IsEnabled = provider.IsEnabled,
            Priority = provider.Priority,
            Config = provider.Config
        };
    }

    private string GetFirstEnabledKey(AiProvider provider, string? keyId = null)
    {
        var config = AiProviderConfigParser.Parse(provider.Config);
        var keys = config.Keys
            .Where(k => k.IsEnabled && !string.IsNullOrWhiteSpace(k.Value))
            .ToList();

        if (!string.IsNullOrWhiteSpace(keyId))
            keys = keys.Where(k => k.Id.Equals(keyId, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var key in keys)
        {
            var plain = SafeDecrypt(key.Value);
            if (!string.IsNullOrWhiteSpace(plain))
                return plain;
        }

        var legacy = SafeDecrypt(provider.EncryptedApiKey);
        return legacy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "";
    }

    private string SafeDecrypt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        try
        {
            var decrypted = _encryption.Decrypt(value);
            return string.IsNullOrWhiteSpace(decrypted) ? value : decrypted;
        }
        catch
        {
            return value;
        }
    }

    private static AiProviderAdvancedConfigDto CloneConfig(AiProviderAdvancedConfigDto config)
    {
        return new AiProviderAdvancedConfigDto
        {
            Protocol = config.Protocol,
            Prefix = config.Prefix,
            ModelsPath = config.ModelsPath,
            ChatPath = config.ChatPath,
            Headers = config.Headers.Select(h => new AiProviderHeaderConfigDto
            {
                Name = h.Name,
                Value = h.Value,
                Enabled = h.Enabled
            }).ToList(),
            Models = config.Models.Select(m => new AiProviderModelConfigDto
            {
                Id = m.Id,
                Name = m.Name,
                Alias = m.Alias,
                IsEnabled = m.IsEnabled,
                IsDefault = m.IsDefault
            }).ToList(),
            Keys = config.Keys.Select(k => new AiProviderKeyConfigDto
            {
                Id = k.Id,
                Value = k.Value,
                MaskedValue = k.MaskedValue,
                ProxyUrl = k.ProxyUrl,
                IsEnabled = k.IsEnabled,
                Status = k.Status,
                LastTestedAt = k.LastTestedAt
            }).ToList()
        };
    }

    private static void ApplyRequestHeaders(HttpClient httpClient, List<AiProviderHeaderConfigDto>? headers, string? apiKey)
    {
        var customHeaders = headers?
            .Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Name))
            .ToList() ?? new List<AiProviderHeaderConfigDto>();

        var hasAuthHeader = customHeaders.Any(h =>
            h.Name.Equals("authorization", StringComparison.OrdinalIgnoreCase)
            || h.Name.Equals("api-key", StringComparison.OrdinalIgnoreCase)
            || h.Name.Equals("x-api-key", StringComparison.OrdinalIgnoreCase));

        if (!hasAuthHeader && !string.IsNullOrWhiteSpace(apiKey))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

        foreach (var header in customHeaders)
        {
            var value = (header.Value ?? "")
                .Replace("{apiKey}", apiKey ?? "", StringComparison.OrdinalIgnoreCase)
                .Replace("${apiKey}", apiKey ?? "", StringComparison.OrdinalIgnoreCase)
                .Replace("{{apiKey}}", apiKey ?? "", StringComparison.OrdinalIgnoreCase);
            httpClient.DefaultRequestHeaders.Remove(header.Name);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Name, value);
        }
    }
}

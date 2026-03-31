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
        return providers.Select(p => new AiProviderDto
        {
            Id = p.Id,
            Name = p.Name,
            DisplayName = p.DisplayName,
            Type = p.Type,
            ApiUrl = p.ApiUrl,
            ApiKey = "",
            IsEnabled = p.IsEnabled,
            Priority = p.Priority,
            Config = p.Config,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }

    public async Task<AiProviderDto?> GetProviderByIdAsync(long id)
    {
        var provider = await _dbContext.AiProviderDb.Where(p => p.Id == id).FirstAsync();
        if (provider == null) return null;

        return new AiProviderDto
        {
            Id = provider.Id,
            Name = provider.Name,
            DisplayName = provider.DisplayName,
            Type = provider.Type,
            ApiUrl = provider.ApiUrl,
            ApiKey = "",
            IsEnabled = provider.IsEnabled,
            Priority = provider.Priority,
            Config = provider.Config,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt
        };
    }

    public async Task<AiProviderDto> CreateProviderAsync(CreateAiProviderRequest request)
    {
        var provider = new AiProvider
        {
            Name = request.Name.ToLower(),
            DisplayName = request.DisplayName,
            Type = request.Type,
            ApiUrl = request.ApiUrl ?? "",
            EncryptedApiKey = _encryption.Encrypt(request.ApiKey),
            IsEnabled = request.IsEnabled,
            Priority = request.Priority,
            Config = request.Config
        };

        provider.Id = await _dbContext.Db.Insertable(provider).ExecuteReturnIdentityAsync();
        
        await InitializeKeyPoolsAsync();

        return new AiProviderDto
        {
            Id = provider.Id,
            Name = provider.Name,
            DisplayName = provider.DisplayName,
            Type = provider.Type,
            ApiUrl = provider.ApiUrl,
            ApiKey = "",
            IsEnabled = provider.IsEnabled,
            Priority = provider.Priority,
            Config = provider.Config,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt
        };
    }

    public async Task<AiProviderDto> UpdateProviderAsync(long id, UpdateAiProviderRequest request)
    {
        var provider = await _dbContext.AiProviderDb.Where(p => p.Id == id).FirstAsync();
        if (provider == null)
            throw new Exception("Provider not found");

        provider.DisplayName = request.DisplayName;
        provider.Type = request.Type;
        provider.ApiUrl = request.ApiUrl ?? "";
        if (!string.IsNullOrEmpty(request.ApiKey))
            provider.EncryptedApiKey = _encryption.Encrypt(request.ApiKey);
        provider.IsEnabled = request.IsEnabled;
        provider.Priority = request.Priority;
        provider.Config = request.Config;
        provider.UpdatedAt = DateTime.Now;

        await _dbContext.Db.Updateable(provider).ExecuteCommandAsync();
        
        await InitializeKeyPoolsAsync();

        return new AiProviderDto
        {
            Id = provider.Id,
            Name = provider.Name,
            DisplayName = provider.DisplayName,
            Type = provider.Type,
            ApiUrl = provider.ApiUrl,
            ApiKey = "",
            IsEnabled = provider.IsEnabled,
            Priority = provider.Priority,
            Config = provider.Config,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt
        };
    }

    public async Task<bool> DeleteProviderAsync(long id)
    {
        var result = await _dbContext.Db.Deleteable<AiProvider>().Where(p => p.Id == id).ExecuteCommandAsync();
        await InitializeKeyPoolsAsync();
        return result > 0;
    }

    public async Task<bool> TestConnectionAsync(long id)
    {
        var provider = await _dbContext.AiProviderDb.Where(p => p.Id == id).FirstAsync();
        if (provider == null)
            return false;

        var aiProvider = _registry.Get(provider.Name);
        if (aiProvider == null)
            return false;

        var apiKey = _encryption.Decrypt(provider.EncryptedApiKey);
        
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == provider.EncryptedApiKey)
        {
            _logger.LogWarning("TestConnection: Invalid API key for provider {Name} (decryption failed or empty)", provider.Name);
            return false;
        }
        
        return await aiProvider.TestConnectionAsync(provider.ApiUrl, apiKey);
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
        var configs = providers.Select(p => new AiProviderConfig
        {
            Id = p.Id,
            Name = p.Name,
            DisplayName = p.DisplayName,
            Type = Enum.TryParse<AiProviderType>(p.Type, true, out var type) ? type : AiProviderType.Chat,
            ApiUrl = p.ApiUrl,
            EncryptedApiKey = p.EncryptedApiKey,
            IsEnabled = p.IsEnabled,
            Priority = p.Priority,
            Config = p.Config
        }).ToList();

        _selector.InitializeKeyPools(configs);
    }
}
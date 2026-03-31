using Microsoft.Extensions.Logging;
using SqlSugar;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI.Legacy;

public class AiMigrationService
{
    private readonly DbContext _dbContext;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly ILogger<AiMigrationService> _logger;

    public AiMigrationService(DbContext dbContext, IAiKeyEncryptionService encryption, ILogger<AiMigrationService> logger)
    {
        _dbContext = dbContext;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<bool> MigrateIfNeededAsync()
    {
        try
        {
            var newTableExists = await _dbContext.Db.Queryable<AiProvider>().AnyAsync();
            if (newTableExists)
            {
                _logger.LogInformation("AI Provider table already exists, skipping migration");
                return true;
            }

            var legacyModels = await _dbContext.Db.Queryable<AiModel>().ToListAsync();
            
            if (legacyModels.Count == 0)
            {
                _logger.LogInformation("No legacy AI models found, creating empty provider table");
                return true;
            }

            _logger.LogInformation("Starting migration of {Count} AI models", legacyModels.Count);

            var providers = new List<AiProvider>();
            foreach (var model in legacyModels)
            {
                var provider = new AiProvider
                {
                    Name = model.Type?.ToLower() ?? "openai",
                    DisplayName = model.Name ?? model.Type ?? "Unknown",
                    Type = "chat",
                    ApiUrl = model.ApiUrl ?? "",
                    EncryptedApiKey = _encryption.Encrypt(model.ApiKey ?? ""),
                    IsEnabled = model.IsEnabled,
                    Priority = model.IsDefault ? 1 : 100,
                    Config = System.Text.Json.JsonSerializer.Serialize(new { model = model.Model, remark = model.Remark })
                };
                providers.Add(provider);
            }

            await _dbContext.Db.Insertable(providers).ExecuteCommandAsync();
            
            _logger.LogInformation("Migration completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            return false;
        }
    }

    public async Task<bool> IsMigrationNeededAsync()
    {
        try
        {
            var newTableExists = await _dbContext.Db.Queryable<AiProvider>().AnyAsync();
            if (newTableExists)
                return false;

            var legacyCount = await _dbContext.Db.Queryable<AiModel>().CountAsync();
            return legacyCount > 0;
        }
        catch
        {
            return false;
        }
    }
}

public class LegacyCompatibilityService
{
    private readonly DbContext _dbContext;
    private readonly ProviderRegistry _registry;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly AiProviderSelector _selector;

    public LegacyCompatibilityService(
        DbContext dbContext,
        ProviderRegistry registry,
        IAiKeyEncryptionService encryption,
        AiProviderSelector selector)
    {
        _dbContext = dbContext;
        _registry = registry;
        _encryption = encryption;
        _selector = selector;
    }

    public async Task InitializeAsync()
    {
        var providers = await _dbContext.AiProviderDb.ToListAsync();
        var configs = providers.Select(p => new AiProviderConfig
        {
            Id = p.Id,
            Name = p.Name,
            DisplayName = p.DisplayName,
            Type = Enum.Parse<AiProviderType>(p.Type, true),
            ApiUrl = p.ApiUrl,
            EncryptedApiKey = p.EncryptedApiKey,
            IsEnabled = p.IsEnabled,
            Priority = p.Priority,
            Config = p.Config
        }).ToList();

        _selector.InitializeKeyPools(configs);
    }

    public async Task<List<AiModel>> GetLegacyModelsAsync()
    {
        return await _dbContext.Db.Queryable<AiModel>().ToListAsync();
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.AI;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/ai/provider")]
[ApiController]
[Authorize]
[RequireRole("admin")]
public class AiProviderController : ControllerBase
{
    private readonly IAiProviderService _providerService;
    private readonly ILogger<AiProviderController> _logger;
    private readonly AiProviderSelector _selector;
    private readonly ProviderRegistry _registry;

    public AiProviderController(IAiProviderService providerService, ILogger<AiProviderController> logger, AiProviderSelector selector, ProviderRegistry registry)
    {
        _providerService = providerService;
        _logger = logger;
        _selector = selector;
        _registry = registry;
    }

    [HttpGet("list")]
    public async Task<Result<List<AiProviderDto>>> GetList()
    {
        var list = await _providerService.GetAllProvidersAsync();
        return Result<List<AiProviderDto>>.Ok(list);
    }

    [HttpGet("enabled")]
    public async Task<Result<List<AiProviderDto>>> GetEnabledProviders()
    {
        var list = await _providerService.GetAllProvidersAsync();
        var enabledList = list.Where(p => p.IsEnabled).ToList();
        return Result<List<AiProviderDto>>.Ok(enabledList);
    }

    [HttpGet("{id:long}")]
    public async Task<Result<AiProviderDto>> GetById(long id)
    {
        var provider = await _providerService.GetProviderByIdAsync(id);
        if (provider == null)
            return Result<AiProviderDto>.Fail("Provider not found");
        return Result<AiProviderDto>.Ok(provider);
    }

    [HttpPost]
    public async Task<Result<AiProviderDto>> Create([FromBody] CreateAiProviderRequest request)
    {
        try
        {
            var provider = await _providerService.CreateProviderAsync(request);
            return Result<AiProviderDto>.Ok(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create provider");
            return Result<AiProviderDto>.Fail(ex.Message);
        }
    }

    [HttpPut("{id:long}")]
    public async Task<Result<AiProviderDto>> Update(long id, [FromBody] UpdateAiProviderRequest request)
    {
        try
        {
            var provider = await _providerService.UpdateProviderAsync(id, request);
            return Result<AiProviderDto>.Ok(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provider");
            return Result<AiProviderDto>.Fail(ex.Message);
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<Result> Delete(long id)
    {
        try
        {
            var success = await _providerService.DeleteProviderAsync(id);
            return success ? Result.Ok() : Result.Fail("Delete failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete provider");
            return Result.Fail(ex.Message);
        }
    }

    [HttpPost("{id:long}/test")]
    public async Task<Result<bool>> TestConnection(long id)
    {
        try
        {
            var success = await _providerService.TestConnectionAsync(id);
            return Result<bool>.Ok(success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connection");
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpPost("migrate")]
    public async Task<Result<bool>> Migrate()
    {
        try
        {
            await _providerService.MigrateLegacyDataAsync();
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpPost("init")]
    public async Task<Result<bool>> Initialize()
    {
        try
        {
            await _providerService.InitializeKeyPoolsAsync();
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialization failed");
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpPost("fetch-models")]
    public async Task<Result<List<object>>> FetchModels([FromBody] FetchModelsRequest request)
    {
        if (string.IsNullOrEmpty(request.ApiUrl) || string.IsNullOrEmpty(request.ApiKey))
        {
            return Result<List<object>>.Fail("API URL 和 API Key 不能为空");
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.ApiKey}");

            var baseUrl = request.ApiUrl.TrimEnd('/');
            var modelsUrl = $"{baseUrl}/models";

            var response = await httpClient.GetAsync(modelsUrl);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                
                if (jsonDoc.RootElement.TryGetProperty("data", out var data))
                {
                    var models = new List<object>();
                    foreach (var item in data.EnumerateArray())
                    {
                        models.Add(new
                        {
                            id = item.GetProperty("id").GetString(),
                            name = item.TryGetProperty("name", out var name) ? name.GetString() : item.GetProperty("id").GetString(),
                            created = item.TryGetProperty("created", out var created) ? created.GetInt64() : 0
                        });
                    }
                    return Result<List<object>>.Ok(models);
                }
                
                return Result<List<object>>.Ok(new List<object>());
            }
            else
            {
                return Result<List<object>>.Fail($"请求失败: {response.StatusCode}\n{content}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch models");
            return Result<List<object>>.Fail($"获取模型列表失败: {ex.Message}");
        }
    }

    // ──────── Provider 健康检查 ────────

    [HttpGet("health")]
    public async Task<Result<List<ProviderHealthDto>>> GetHealth(CancellationToken ct)
    {
        var configs = _selector.GetEnabledProviderConfigs();
        var results = new System.Collections.Concurrent.ConcurrentBag<ProviderHealthDto>();

        await Parallel.ForEachAsync(configs, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct }, async (cfg, token) =>
        {
            var provider = _registry.Get(cfg.Name);
            if (provider == null) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            string status = "unknown"; string? error = null;
            try
            {
                var ok = await provider.TestConnectionAsync(cfg.ApiUrl, cfg.ApiKey ?? "", token);
                status = ok ? "healthy" : "unhealthy";
            }
            catch (Exception ex) { status = "error"; error = ex.Message; }
            sw.Stop();

            results.Add(new ProviderHealthDto
            {
                Name        = cfg.Name,
                Status      = status,
                LatencyMs   = sw.ElapsedMilliseconds,
                LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Error       = error
            });
        });

        return Result<List<ProviderHealthDto>>.Ok(results.OrderBy(r => r.Name).ToList());
    }

    [HttpGet("key-pool-status")]
    public Result<List<ProviderKeyPoolStatus>> GetKeyPoolStatus()
    {
        return Result<List<ProviderKeyPoolStatus>>.Ok(_selector.GetKeyPoolStatus());
    }

    [HttpPost("{name}/reset-keys")]
    public Result<bool> ResetKeys(string name)
    {
        _selector.ResetKeyHealth(name);
        return Result<bool>.Ok(true);
    }
}

public class FetchModelsRequest
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class ProviderHealthDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public string LastChecked { get; set; } = string.Empty;
    public string? Error { get; set; }
}
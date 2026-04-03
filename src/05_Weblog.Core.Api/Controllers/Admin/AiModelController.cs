using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/ai-model")]
[ApiController]
//[Authorize(Roles ="admin")]
public class AiModelController : ControllerBase
{
    private readonly IAiModelService _aiModelService;
    private readonly ISqlSugarClient _db;

    public AiModelController(IAiModelService aiModelService, ISqlSugarClient db)
    {
        _aiModelService = aiModelService;
        _db = db;
    }

    [HttpGet]
    public async Task<Result<List<AiModelDto>>> GetAll()
    {
        var result = await _aiModelService.GetAllAsync();
        return Result<List<AiModelDto>>.Ok(result);
    }

    [HttpGet("enabled-list")]
    public async Task<Result<List<AiModelDto>>> GetEnabledList()
    {
        var result = await _aiModelService.GetAllEnabledAsync();
        return Result<List<AiModelDto>>.Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<Result<AiModelDto>> Get(long id)
    {
        var result = await _aiModelService.GetByIdAsync(id);
        return Result<AiModelDto>.Ok(result ?? new AiModelDto());
    }

    [HttpPost]
    [RequireRole("admin")]
    public async Task<Result<AiModelDto>> Create([FromBody] CreateAiModelRequest request)
    {
        var result = await _aiModelService.CreateAsync(request);
        return Result<AiModelDto>.Ok(result);
    }

    [HttpPut]
    [RequireRole("admin")]
    public async Task<Result<AiModelDto>> Update([FromBody] UpdateAiModelRequest request)
    {
        var result = await _aiModelService.UpdateAsync(request);
        return Result<AiModelDto>.Ok(result);
    }

    [HttpDelete("{id:long}")]
    [RequireRole("admin")]
    public async Task<Result<bool>> Delete(long id)
    {
        await _aiModelService.DeleteAsync(id);
        return Result<bool>.Ok(true);
    }

    [HttpPost("test/{id:long}")]
    public async Task<Result<string>> Test(long id)
    {
        try
        {
            var model = await _aiModelService.GetByIdAsync(id);
            if (model == null || string.IsNullOrEmpty(model.ApiKey))
            {
                return Result<string>.Fail("模型不存在或API Key为空");
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            string testUrl = model.ApiUrl;
            var headers = new Dictionary<string, string>();

            switch (model.Type.ToLower())
            {
                case "openai":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/models";
                    headers["Authorization"] = $"Bearer {model.ApiKey}";
                    break;
                case "claude":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/messages";
                    headers["x-api-key"] = model.ApiKey;
                    headers["anthropic-version"] = "2023-06-01";
                    break;
                case "azure":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/models";
                    headers["api-key"] = model.ApiKey;
                    break;
                case "minimax":
                case "minimax-chat":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/v1/models/list";
                    headers["Authorization"] = $"Bearer {model.ApiKey}";
                    break;
                case "gemini":
                    testUrl = $"https://generativelanguage.googleapis.com/v1/models?key={model.ApiKey}";
                    break;
                case "qianfan":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/models";
                    break;
                case "zhipu":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/v4/models";
                    headers["Authorization"] = $"Bearer {model.ApiKey}";
                    break;
                case "deepseek":
                    testUrl = $"{model.ApiUrl.TrimEnd('/')}/models";
                    headers["Authorization"] = $"Bearer {model.ApiKey}";
                    break;
                case "other":
                default:
                    testUrl = model.ApiUrl;
                    break;
            }

            foreach (var header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await httpClient.GetAsync(testUrl);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Result<string>.Ok($"测试成功！模型连接正常。\n响应：{responseBody}");
            }
            else
            {
                return Result<string>.Fail($"测试失败：HTTP {response.StatusCode}\n响应：{responseBody}");
            }
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"测试失败：{ex.Message}");
        }
    }

    // ── 统计 & 趋势 ─────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<Result<object>> GetStats()
    {
        try
        {
            var usageLogs = await _db.Queryable<Model.Entities.AiUsageLog>().ToListAsync();
            var totalTokens = usageLogs.Sum(u => u.TokensUsed ?? 0);
            var totalRequests = usageLogs.Sum(u => u.UsageCount);
            return Result<object>.Ok(new { totalTokens, totalRequests });
        }
        catch
        {
            return Result<object>.Ok(new { totalTokens = 0, totalRequests = 0 });
        }
    }

    [HttpGet("trend")]
    public async Task<Result<List<object>>> GetTrend([FromQuery] int days = 30)
    {
        try
        {
            var since = DateTime.Now.AddDays(-days);
            var logs = await _db.Queryable<Model.Entities.AiUsageLog>()
                .Where(u => u.UsageDate >= since)
                .OrderBy(u => u.UsageDate)
                .ToListAsync();

            var grouped = logs
                .GroupBy(u => u.UsageDate.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    tokens = g.Sum(u => u.TokensUsed ?? 0),
                    requests = g.Sum(u => u.UsageCount)
                })
                .ToList();

            // Fill missing dates with zero
            var result = new List<object>();
            for (var d = since.Date; d <= DateTime.Now.Date; d = d.AddDays(1))
            {
                var existing = grouped.FirstOrDefault(g => g.date == d.ToString("yyyy-MM-dd"));
                result.Add(new { date = d.ToString("yyyy-MM-dd"), tokens = existing?.tokens ?? 0, requests = existing?.requests ?? 0 });
            }
            return Result<List<object>>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<List<object>>.Fail(ex.Message);
        }
    }

    // ── 批量操作 ─────────────────────────────────────────

    [HttpPut("batch")]
    [RequireRole("admin")]
    public async Task<Result<bool>> BatchUpdate([FromBody] BatchModelUpdateRequest req)
    {
        foreach (var id in req.Ids)
        {
            try
            {
                var model = await _aiModelService.GetByIdAsync(id);
                if (model != null)
                {
                    await _aiModelService.UpdateAsync(new UpdateAiModelRequest
                    {
                        Id = id,
                        Name = model.Name,
                        Type = model.Type,
                        Model = model.Model,
                        ApiUrl = model.ApiUrl,
                        ApiKey = model.ApiKey,
                        IsDefault = model.IsDefault,
                        IsEnabled = req.IsEnabled,
                        Remark = model.Remark
                    });
                }
            }
            catch { /* skip individual failures */ }
        }
        return Result<bool>.Ok(true);
    }
}

public class BatchModelUpdateRequest
{
    public List<long> Ids { get; set; } = new();
    public bool IsEnabled { get; set; }
}

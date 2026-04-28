using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Api.Filters;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.AI;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/ai/assistant")]
[ApiController]
[Authorize]
[RequireRole("admin")]
public class AiAssistantController : ControllerBase
{
    private readonly IAiAssistantService _assistantService;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(IAiAssistantService assistantService, ILogger<AiAssistantController> logger)
    {
        _assistantService = assistantService;
        _logger = logger;
    }

    [HttpGet("usage")]
    public async Task<Result<List<AiUsageStatDto>>> GetUsageStats()
    {
        try
        {
            var stats = await _assistantService.GetUsageStatsAsync();
            return Result<List<AiUsageStatDto>>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI usage stats");
            return Result<List<AiUsageStatDto>>.Fail(ex.Message);
        }
    }

    [HttpGet("sessions")]
    public async Task<Result<List<AiSessionDto>>> GetSessions()
    {
        try
        {
            var sessions = await _assistantService.GetSessionsAsync();
            return Result<List<AiSessionDto>>.Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI sessions");
            return Result<List<AiSessionDto>>.Fail(ex.Message);
        }
    }

    [HttpDelete("session/{sessionId}")]
    public async Task<Result<bool>> DeleteSession(string sessionId)
    {
        try
        {
            var deleted = await _assistantService.DeleteSessionAsync(sessionId);
            return Result<bool>.Ok(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete AI session: {SessionId}", sessionId);
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpGet("settings")]
    public async Task<Result<AiAssistantSettingsDto>> GetSettings()
    {
        try
        {
            var settings = await _assistantService.GetSettingsAsync();
            return Result<AiAssistantSettingsDto>.Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI assistant settings");
            return Result<AiAssistantSettingsDto>.Fail(ex.Message);
        }
    }

    [HttpPut("settings")]
    public async Task<Result<bool>> UpdateSettings([FromBody] AiAssistantSettingsDto settings)
    {
        try
        {
            var updated = await _assistantService.UpdateSettingsAsync(settings);
            return Result<bool>.Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update AI assistant settings");
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpPost("generate-article")]
    public async Task<Result<string>> GenerateArticle([FromBody] GenerateArticleRequest request)
    {
        try
        {
            var article = await _assistantService.GenerateArticleAsync(request);
            return Result<string>.Ok(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate article with AI assistant");
            return Result<string>.Fail(ex.Message);
        }
    }

    [HttpPost("seo-optimize")]
    public async Task<Result<SeoOptimizeResultDto>> SeoOptimize([FromBody] SeoOptimizeRequest request)
    {
        try
        {
            var result = await _assistantService.OptimizeSeoAsync(request);
            return Result<SeoOptimizeResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize SEO with AI assistant");
            return Result<SeoOptimizeResultDto>.Fail(ex.Message);
        }
    }

    [HttpPost("moderate")]
    public async Task<Result<ModerationResultDto>> ModerateContent([FromBody] ModerateRequest request)
    {
        try
        {
            var result = await _assistantService.ModerateContentAsync(request);
            return Result<ModerationResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to moderate content with AI assistant");
            return Result<ModerationResultDto>.Fail(ex.Message);
        }
    }

    [HttpGet("token-stats")]
    public async Task<Result<List<TokenStatDto>>> GetTokenStats([FromQuery] int days = 7)
    {
        try
        {
            var stats = await _assistantService.GetTokenStatsAsync(days);
            return Result<List<TokenStatDto>>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI token stats");
            return Result<List<TokenStatDto>>.Fail(ex.Message);
        }
    }
}

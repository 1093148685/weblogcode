using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Service.AI.Rag;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/rag")]
[ApiController]
[Authorize]
public class RagKnowledgeController : ControllerBase
{
    private readonly IRagAdminService _ragAdminService;
    private readonly ILogger<RagKnowledgeController> _logger;

    public RagKnowledgeController(IRagAdminService ragAdminService, ILogger<RagKnowledgeController> logger)
    {
        _ragAdminService = ragAdminService;
        _logger = logger;
    }

    [HttpGet("knowledge-base/list")]
    public async Task<Result<List<KbListItem>>> GetKbList()
    {
        var items = await _ragAdminService.GetKnowledgeBasesAsync();
        return Result<List<KbListItem>>.Ok(items);
    }

    [HttpPost("knowledge-base")]
    public async Task<Result<KbListItem>> CreateKb([FromBody] CreateKbRequest request)
    {
        try
        {
            var item = await _ragAdminService.CreateKnowledgeBaseAsync(request);
            return Result<KbListItem>.Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create knowledge base failed");
            return Result<KbListItem>.Fail(ex.Message);
        }
    }

    [HttpPut("knowledge-base/{id:long}")]
    public async Task<Result<bool>> UpdateKb(long id, [FromBody] CreateKbRequest request)
    {
        try
        {
            await _ragAdminService.UpdateKnowledgeBaseAsync(id, request);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update knowledge base {KbId} failed", id);
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpDelete("knowledge-base/{id:long}")]
    public async Task<Result<bool>> DeleteKb(long id)
    {
        try
        {
            await _ragAdminService.DeleteKnowledgeBaseAsync(id);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete knowledge base {KbId} failed", id);
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpGet("knowledge-base/{kbId:long}/documents")]
    public async Task<Result<List<KbDocumentDto>>> GetDocuments(
        long kbId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var documents = await _ragAdminService.GetDocumentsAsync(kbId, page, pageSize);
        return Result<List<KbDocumentDto>>.Ok(documents);
    }

    [HttpPost("knowledge-base/{kbId:long}/import/articles")]
    public async Task<Result<ImportResult>> ImportArticles(long kbId, [FromBody] ImportArticlesRequest request)
    {
        var result = await _ragAdminService.ImportArticlesAsync(kbId, request);
        return Result<ImportResult>.Ok(result);
    }

    [HttpPost("knowledge-base/{kbId:long}/import/wiki")]
    public async Task<Result<ImportResult>> ImportWiki(long kbId, [FromBody] ImportWikiRequest request)
    {
        var result = await _ragAdminService.ImportWikiAsync(kbId, request);
        return Result<ImportResult>.Ok(result);
    }

    [HttpPost("knowledge-base/{kbId:long}/upload")]
    public async Task<Result<KbDocumentDto>> UploadDocument(long kbId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Result<KbDocumentDto>.Fail("文件不能为空");

        if (file.Length > 10 * 1024 * 1024)
            return Result<KbDocumentDto>.Fail("文件大小不能超过 10MB");

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var document = await _ragAdminService.UploadTextAsync(kbId, file.FileName, content);

        return Result<KbDocumentDto>.Ok(document);
    }

    [HttpDelete("knowledge-base/{kbId:long}/documents/{docId:long}")]
    public async Task<Result<bool>> DeleteDocument(long kbId, long docId)
    {
        await _ragAdminService.DeleteDocumentAsync(kbId, docId);
        return Result<bool>.Ok(true);
    }

    [HttpPost("knowledge-base/{kbId:long}/documents/{docId:long}/reindex")]
    public async Task<Result<bool>> ReindexDocument(long kbId, long docId)
    {
        await _ragAdminService.QueueReindexDocumentAsync(docId);
        return Result<bool>.Ok(true);
    }

    [HttpPost("knowledge-base/{kbId:long}/reindex")]
    public async Task<Result<bool>> ReindexAll(long kbId)
    {
        await _ragAdminService.QueueReindexAllAsync(kbId);
        return Result<bool>.Ok(true);
    }

    [HttpPost("knowledge-base/{kbId:long}/retrieval-test")]
    public async Task<Result<RetrievalTestResult>> RetrievalTest(long kbId, [FromBody] RetrievalTestRequest request)
    {
        try
        {
            var result = await _ragAdminService.RetrievalTestAsync(kbId, request);
            return Result<RetrievalTestResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retrieval test failed for kb {KbId}", kbId);
            return Result<RetrievalTestResult>.Fail(ex.Message);
        }
    }

    [HttpPost("knowledge-base/{kbId:long}/upload-file")]
    [RequestSizeLimit(10_485_760)]
    public async Task<Result<KbDocumentDto>> UploadFile(long kbId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Result<KbDocumentDto>.Fail("请选择文件");

        try
        {
            await using var stream = file.OpenReadStream();
            var document = await _ragAdminService.UploadFileAsync(kbId, file.FileName, stream);
            return Result<KbDocumentDto>.Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload file failed for kb {KbId}", kbId);
            return Result<KbDocumentDto>.Fail(ex.Message);
        }
    }

    [HttpGet("knowledge-base/{kbId:long}/stats")]
    public async Task<Result<KbStats>> GetStats(long kbId)
    {
        var stats = await _ragAdminService.GetStatsAsync(kbId);
        return Result<KbStats>.Ok(stats);
    }
}

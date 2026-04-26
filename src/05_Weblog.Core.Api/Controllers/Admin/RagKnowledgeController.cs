using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Weblog.Core.Common.Result;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI.Rag;

namespace Weblog.Core.Api.Controllers.Admin;

[Route("api/admin/rag")]
[ApiController]
[Authorize]
public class RagKnowledgeController : ControllerBase
{
    private readonly IRagService _rag;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<RagKnowledgeController> _logger;

    public RagKnowledgeController(IRagService rag, ISqlSugarClient db, ILogger<RagKnowledgeController> logger)
    {
        _rag = rag;
        _db = db;
        _logger = logger;
    }

    // ── 知识库 CRUD ──────────────────────────────────────────────────────

    [HttpGet("knowledge-base/list")]
    public async Task<Result<List<KbListItem>>> GetKbList()
    {
        var kbs = await _rag.GetAllKbsAsync();
        var items = kbs.Select(k => new KbListItem
        {
            Id = k.Id,
            Name = k.Name,
            Description = k.Description,
            EmbeddingProvider = k.EmbeddingProvider,
            EmbeddingModel = k.EmbeddingModel,
            ChunkSize = k.ChunkSize,
            ChunkOverlap = k.ChunkOverlap,
            IsEnabled = k.IsEnabled,
            CreatedAt = k.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
        return Result<List<KbListItem>>.Ok(items);
    }

    [HttpPost("knowledge-base")]
    public async Task<Result<KbListItem>> CreateKb([FromBody] CreateKbRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Result<KbListItem>.Fail("知识库名称不能为空");

        var kb = await _rag.CreateKbAsync(req);
        return Result<KbListItem>.Ok(new KbListItem
        {
            Id = kb.Id,
            Name = kb.Name,
            Description = kb.Description,
            EmbeddingProvider = kb.EmbeddingProvider,
            EmbeddingModel = kb.EmbeddingModel,
            ChunkSize = kb.ChunkSize,
            ChunkOverlap = kb.ChunkOverlap,
            IsEnabled = kb.IsEnabled,
            CreatedAt = kb.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    [HttpPut("knowledge-base/{id:long}")]
    public async Task<Result<bool>> UpdateKb(long id, [FromBody] CreateKbRequest req)
    {
        try
        {
            await _rag.UpdateKbAsync(id, req);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    [HttpDelete("knowledge-base/{id:long}")]
    public async Task<Result<bool>> DeleteKb(long id)
    {
        await _rag.DeleteKbAsync(id);
        return Result<bool>.Ok(true);
    }

    // ── 文档管理 ─────────────────────────────────────────────────────────

    [HttpGet("knowledge-base/{kbId:long}/documents")]
    public async Task<Result<List<KbDocumentDto>>> GetDocuments(
        long kbId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var docs = await _rag.GetDocumentsAsync(kbId, page, pageSize);
        var dtos = docs.Select(d => ToDocDto(d)).ToList();
        return Result<List<KbDocumentDto>>.Ok(dtos);
    }

    [HttpPost("knowledge-base/{kbId:long}/import/articles")]
    public async Task<Result<ImportResult>> ImportArticles(
        long kbId, [FromBody] ImportArticlesRequest req)
    {
        if (req.ArticleIds.Count == 0)
            return Result<ImportResult>.Ok(new ImportResult { Imported = 0, Total = 0 });

        // 批量查询，避免 N 次串行数据库请求
        var articles = await _db.Queryable<Article>()
            .Where(a => req.ArticleIds.Contains(a.Id))
            .ToListAsync();
        var contents = await _db.Queryable<ArticleContent>()
            .Where(c => req.ArticleIds.Contains(c.ArticleId))
            .ToListAsync();

        var contentMap = contents.ToDictionary(c => c.ArticleId, c => c.Content ?? "");

        var docIds = new List<long>();
        foreach (var article in articles)
        {
            try
            {
                if (!contentMap.TryGetValue(article.Id, out var text)) continue;
                var doc = await _rag.AddDocumentAsync(kbId, article.Title, text, "article", article.Id);
                docIds.Add(doc.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import article {ArticleId} failed", article.Id);
            }
        }

        // 后台串行索引：避免同时并发 N 个 Embedding 请求触发限流
        _ = Task.Run(async () =>
        {
            foreach (var docId in docIds)
            {
                try { await _rag.IndexDocumentAsync(docId); }
                catch (Exception ex) { _logger.LogWarning(ex, "Background indexing failed for doc {DocId}", docId); }
            }
        });

        return Result<ImportResult>.Ok(new ImportResult { Imported = docIds.Count, Total = req.ArticleIds.Count });
    }

    [HttpPost("knowledge-base/{kbId:long}/import/wiki")]
    public async Task<Result<ImportResult>> ImportWiki(
        long kbId, [FromBody] ImportWikiRequest req)
    {
        var imported = 0;
        foreach (var wikiId in req.WikiIds)
        {
            try
            {
                var wiki = await _db.Queryable<Wiki>().FirstAsync(w => w.Id == wikiId);
                if (wiki == null) continue;

                // Wiki 可能有多个 Catalog，每个 catalog 作为一个文档
                var catalogs = await _db.Queryable<WikiCatalog>()
                    .Where(c => c.WikiId == wikiId)
                    .ToListAsync();

                if (catalogs.Count == 0)
                {
                    // 没有 catalog 则整个 wiki 作为一个文档
                    var doc = await _rag.AddDocumentAsync(kbId, wiki.Title, wiki.Summary ?? wiki.Title, "wiki", wikiId);
                    _ = Task.Run(() => _rag.IndexDocumentAsync(doc.Id));
                    imported++;
                }
                else
                {
                    foreach (var cat in catalogs.Where(c => c.ArticleId.HasValue))
                    {
                        // WikiCatalog 关联文章，从文章内容中读取
                        var articleContent = await _db.Queryable<ArticleContent>()
                            .FirstAsync(a => a.ArticleId == cat.ArticleId!.Value);
                        var docTitle = $"{wiki.Title} - {cat.Title}";
                        var content = articleContent?.Content ?? cat.Title;
                        var doc = await _rag.AddDocumentAsync(kbId, docTitle, content, "wiki", wikiId);
                        _ = Task.Run(() => _rag.IndexDocumentAsync(doc.Id));
                        imported++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import wiki {WikiId} failed", wikiId);
            }
        }
        return Result<ImportResult>.Ok(new ImportResult { Imported = imported, Total = req.WikiIds.Count });
    }

    [HttpPost("knowledge-base/{kbId:long}/upload")]
    public async Task<Result<KbDocumentDto>> UploadDocument(
        long kbId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Result<KbDocumentDto>.Fail("文件不能为空");

        if (file.Length > 10 * 1024 * 1024)
            return Result<KbDocumentDto>.Fail("文件大小不能超过 10MB");

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();

        var doc = await _rag.AddDocumentAsync(
            kbId,
            Path.GetFileNameWithoutExtension(file.FileName),
            content,
            "upload");

        _ = Task.Run(() => _rag.IndexDocumentAsync(doc.Id));

        return Result<KbDocumentDto>.Ok(ToDocDto(doc));
    }

    [HttpDelete("knowledge-base/{kbId:long}/documents/{docId:long}")]
    public async Task<Result<bool>> DeleteDocument(long kbId, long docId)
    {
        await _rag.DeleteDocumentAsync(kbId, docId);
        return Result<bool>.Ok(true);
    }

    [HttpPost("knowledge-base/{kbId:long}/documents/{docId:long}/reindex")]
    public async Task<Result<bool>> ReindexDocument(long kbId, long docId)
    {
        _ = Task.Run(() => _rag.IndexDocumentAsync(docId));
        return Result<bool>.Ok(true);
    }

    [HttpPost("knowledge-base/{kbId:long}/reindex")]
    public async Task<Result<bool>> ReindexAll(long kbId)
    {
        _ = Task.Run(() => _rag.ReindexAllAsync(kbId));
        return Result<bool>.Ok(true);
    }

    // ── 检索测试 ─────────────────────────────────────────────────────────

    [HttpPost("knowledge-base/{kbId:long}/retrieval-test")]
    public async Task<Result<RetrievalTestResult>> RetrievalTest(
        long kbId, [FromBody] RetrievalTestRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Query))
            return Result<RetrievalTestResult>.Fail("查询内容不能为空");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var topK = req.TopK > 0 ? req.TopK : 5;
            var results = await _rag.RetrieveAsync(kbId, req.Query, topK,
                req.VectorWeight > 0 ? req.VectorWeight : 0.7f,
                req.KeywordWeight > 0 ? req.KeywordWeight : 0.3f);
            sw.Stop();
            return Result<RetrievalTestResult>.Ok(new RetrievalTestResult
            {
                Chunks          = results,
                RetrievalTimeMs = sw.ElapsedMilliseconds,
                TotalFound      = results.Count,
                Query           = req.Query,
                VectorWeight    = req.VectorWeight > 0 ? req.VectorWeight : 0.7f,
                KeywordWeight   = req.KeywordWeight > 0 ? req.KeywordWeight : 0.3f
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retrieval test failed for kb {KbId}", kbId);
            return Result<RetrievalTestResult>.Fail(ex.Message);
        }
    }

    // ── 文件上传（TXT / MD / 纯文本 PDF） ───────────────────────────────────

    [HttpPost("knowledge-base/{kbId:long}/upload-file")]
    [RequestSizeLimit(10_485_760)] // 10MB
    public async Task<Result<KbDocumentDto>> UploadFile(long kbId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Result<KbDocumentDto>.Fail("请选择文件");

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext is not (".txt" or ".md" or ".pdf"))
            return Result<KbDocumentDto>.Fail("仅支持 .txt / .md / .pdf 文件");

        try
        {
            string content;
            using var stream = file.OpenReadStream();

            if (ext == ".pdf")
            {
                // 简单 PDF：按行读取文本（不依赖外部库，提取可打印字符）
                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var raw = await reader.ReadToEndAsync();
                // 提取可打印的中英文字符，过滤 PDF 二进制
                content = new string(raw.Where(c => c >= ' ' && c != (char)0x7F).ToArray());
                if (content.Length < 50)
                    return Result<KbDocumentDto>.Fail("PDF 文本提取内容过少，请使用文本可复制的 PDF");
            }
            else
            {
                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                content = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(content))
                return Result<KbDocumentDto>.Fail("文件内容为空");

            var doc = await _rag.ImportTextDocumentAsync(kbId,
                Path.GetFileNameWithoutExtension(file.FileName),
                content, "file");
            return Result<KbDocumentDto>.Ok(ToDocDto(doc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadFile failed for kb {KbId}", kbId);
            return Result<KbDocumentDto>.Fail(ex.Message);
        }
    }

    // ── 统计 ─────────────────────────────────────────────────────────────

    [HttpGet("knowledge-base/{kbId:long}/stats")]
    public async Task<Result<KbStats>> GetStats(long kbId)
    {
        var stats = await _rag.GetStatsAsync(kbId);
        return Result<KbStats>.Ok(stats);
    }

    // ── 辅助方法 ─────────────────────────────────────────────────────────

    private static KbDocumentDto ToDocDto(KbDocument d) => new()
    {
        Id = d.Id,
        KbId = d.KbId,
        Title = d.Title,
        SourceType = d.SourceType,
        SourceId = d.SourceId,
        Status = d.Status,
        ErrorMessage = d.ErrorMessage,
        ChunkCount = d.ChunkCount,
        CreatedAt = d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        UpdatedAt = d.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
    };
}

// ── Request / Response DTOs ───────────────────────────────────────────────

public class KbListItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EmbeddingProvider { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int ChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
    public bool IsEnabled { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class KbDocumentDto
{
    public long Id { get; set; }
    public long KbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public long SourceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ChunkCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class ImportArticlesRequest
{
    public List<long> ArticleIds { get; set; } = new();
}

public class ImportWikiRequest
{
    public List<long> WikiIds { get; set; } = new();
}

public class ImportResult
{
    public int Imported { get; set; }
    public int Total { get; set; }
}

public class RetrievalTestRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
    public float VectorWeight { get; set; } = 0.7f;
    public float KeywordWeight { get; set; } = 0.3f;
}

public class RetrievalTestResult
{
    public List<RetrievedChunk> Chunks { get; set; } = new();
    public long RetrievalTimeMs { get; set; }
    public int TotalFound { get; set; }
    public string Query { get; set; } = string.Empty;
    public float VectorWeight { get; set; }
    public float KeywordWeight { get; set; }
}

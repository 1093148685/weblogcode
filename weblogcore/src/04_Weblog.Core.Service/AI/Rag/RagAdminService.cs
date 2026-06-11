using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;

namespace Weblog.Core.Service.AI.Rag;

public interface IRagAdminService
{
    Task<List<KbListItem>> GetKnowledgeBasesAsync();
    Task<KbListItem> CreateKnowledgeBaseAsync(CreateKbRequest request);
    Task UpdateKnowledgeBaseAsync(long id, CreateKbRequest request);
    Task DeleteKnowledgeBaseAsync(long id);
    Task<List<KbDocumentDto>> GetDocumentsAsync(long kbId, int page, int pageSize);
    Task<ImportResult> ImportArticlesAsync(long kbId, ImportArticlesRequest request);
    Task<ImportResult> ImportWikiAsync(long kbId, ImportWikiRequest request);
    Task<KbDocumentDto> UploadTextAsync(long kbId, string fileName, string content, string sourceType = "upload");
    Task<KbDocumentDto> UploadFileAsync(long kbId, string fileName, Stream stream);
    Task DeleteDocumentAsync(long kbId, long docId);
    Task QueueReindexDocumentAsync(long docId);
    Task QueueReindexAllAsync(long kbId);
    Task<RetrievalTestResult> RetrievalTestAsync(long kbId, RetrievalTestRequest request);
    Task<KbStats> GetStatsAsync(long kbId);
}

public class RagAdminService : IRagAdminService
{
    private const int MaxUploadBytes = 10 * 1024 * 1024;
    private const int DefaultTopK = 5;
    private const float DefaultVectorWeight = 0.7f;
    private const float DefaultKeywordWeight = 0.3f;

    private readonly IRagService _rag;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<RagAdminService> _logger;

    public RagAdminService(IRagService rag, ISqlSugarClient db, ILogger<RagAdminService> logger)
    {
        _rag = rag;
        _db = db;
        _logger = logger;
    }

    public async Task<List<KbListItem>> GetKnowledgeBasesAsync()
    {
        var knowledgeBases = await _rag.GetAllKbsAsync();
        return knowledgeBases.Select(RagDtoMapper.ToKbListItem).ToList();
    }

    public async Task<KbListItem> CreateKnowledgeBaseAsync(CreateKbRequest request)
    {
        NormalizeKnowledgeBaseRequest(request);
        var knowledgeBase = await _rag.CreateKbAsync(request);
        return RagDtoMapper.ToKbListItem(knowledgeBase);
    }

    public async Task UpdateKnowledgeBaseAsync(long id, CreateKbRequest request)
    {
        NormalizeKnowledgeBaseRequest(request);
        await _rag.UpdateKbAsync(id, request);
    }

    public Task DeleteKnowledgeBaseAsync(long id)
    {
        return _rag.DeleteKbAsync(id);
    }

    public async Task<List<KbDocumentDto>> GetDocumentsAsync(long kbId, int page, int pageSize)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var documents = await _rag.GetDocumentsAsync(kbId, normalizedPage, normalizedPageSize);
        return documents.Select(RagDtoMapper.ToDocumentDto).ToList();
    }

    public async Task<ImportResult> ImportArticlesAsync(long kbId, ImportArticlesRequest request)
    {
        var articleIds = request.ArticleIds.Distinct().ToList();
        if (articleIds.Count == 0)
            return new ImportResult { Imported = 0, Total = 0 };

        var articles = await _db.Queryable<Article>()
            .Where(article => articleIds.Contains(article.Id))
            .ToListAsync();

        var contents = await _db.Queryable<ArticleContent>()
            .Where(content => articleIds.Contains(content.ArticleId))
            .ToListAsync();

        var contentMap = contents.ToDictionary(content => content.ArticleId, content => content.Content ?? "");
        var documentIds = new List<long>();

        foreach (var article in articles)
        {
            try
            {
                if (!contentMap.TryGetValue(article.Id, out var text) || string.IsNullOrWhiteSpace(text))
                    continue;

                var document = await _rag.AddDocumentAsync(kbId, article.Title, text, "article", article.Id);
                documentIds.Add(document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import article {ArticleId} failed", article.Id);
            }
        }

        QueueSequentialIndexing(documentIds);
        return new ImportResult { Imported = documentIds.Count, Total = articleIds.Count };
    }

    public async Task<ImportResult> ImportWikiAsync(long kbId, ImportWikiRequest request)
    {
        var wikiIds = request.WikiIds.Distinct().ToList();
        if (wikiIds.Count == 0)
            return new ImportResult { Imported = 0, Total = 0 };

        var imported = 0;
        var documentIds = new List<long>();

        foreach (var wikiId in wikiIds)
        {
            try
            {
                var wiki = await _db.Queryable<Wiki>().FirstAsync(item => item.Id == wikiId);
                if (wiki == null)
                    continue;

                var catalogs = await _db.Queryable<WikiCatalog>()
                    .Where(catalog => catalog.WikiId == wikiId)
                    .ToListAsync();

                if (catalogs.Count == 0)
                {
                    var document = await _rag.AddDocumentAsync(kbId, wiki.Title, wiki.Summary ?? wiki.Title, "wiki", wikiId);
                    documentIds.Add(document.Id);
                    imported++;
                    continue;
                }

                foreach (var catalog in catalogs.Where(item => item.ArticleId.HasValue))
                {
                    var articleContent = await _db.Queryable<ArticleContent>()
                        .FirstAsync(content => content.ArticleId == catalog.ArticleId!.Value);
                    var title = $"{wiki.Title} - {catalog.Title}";
                    var content = articleContent?.Content ?? catalog.Title;

                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    var document = await _rag.AddDocumentAsync(kbId, title, content, "wiki", wikiId);
                    documentIds.Add(document.Id);
                    imported++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import wiki {WikiId} failed", wikiId);
            }
        }

        QueueSequentialIndexing(documentIds);
        return new ImportResult { Imported = imported, Total = wikiIds.Count };
    }

    public async Task<KbDocumentDto> UploadTextAsync(long kbId, string fileName, string content, string sourceType = "upload")
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("文件内容不能为空");

        var title = Path.GetFileNameWithoutExtension(fileName);
        var document = await _rag.AddDocumentAsync(kbId, title, content, sourceType);
        QueueIndexing(document.Id);

        return RagDtoMapper.ToDocumentDto(document);
    }

    public async Task<KbDocumentDto> UploadFileAsync(long kbId, string fileName, Stream stream)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".txt" or ".md" or ".pdf"))
            throw new InvalidOperationException("仅支持 .txt / .md / .pdf 文件");

        if (stream.CanSeek && stream.Length > MaxUploadBytes)
            throw new InvalidOperationException("文件大小不能超过 10MB");

        var content = await ReadUploadContentAsync(extension, stream);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("文件内容为空");

        var document = await _rag.ImportTextDocumentAsync(kbId, Path.GetFileNameWithoutExtension(fileName), content, "file");
        return RagDtoMapper.ToDocumentDto(document);
    }

    public Task DeleteDocumentAsync(long kbId, long docId)
    {
        return _rag.DeleteDocumentAsync(kbId, docId);
    }

    public Task QueueReindexDocumentAsync(long docId)
    {
        QueueIndexing(docId);
        return Task.CompletedTask;
    }

    public Task QueueReindexAllAsync(long kbId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _rag.ReindexAllAsync(kbId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background reindex failed for kb {KbId}", kbId);
            }
        });

        return Task.CompletedTask;
    }

    public async Task<RetrievalTestResult> RetrievalTestAsync(long kbId, RetrievalTestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            throw new InvalidOperationException("查询内容不能为空");

        var topK = request.TopK > 0 ? request.TopK : DefaultTopK;
        var vectorWeight = request.VectorWeight > 0 ? request.VectorWeight : DefaultVectorWeight;
        var keywordWeight = request.KeywordWeight > 0 ? request.KeywordWeight : DefaultKeywordWeight;

        var stopwatch = Stopwatch.StartNew();
        var chunks = await _rag.RetrieveAsync(kbId, request.Query, topK, vectorWeight, keywordWeight);
        stopwatch.Stop();

        return new RetrievalTestResult
        {
            Chunks = chunks,
            RetrievalTimeMs = stopwatch.ElapsedMilliseconds,
            TotalFound = chunks.Count,
            Query = request.Query,
            VectorWeight = vectorWeight,
            KeywordWeight = keywordWeight
        };
    }

    public Task<KbStats> GetStatsAsync(long kbId)
    {
        return _rag.GetStatsAsync(kbId);
    }

    private void QueueSequentialIndexing(IEnumerable<long> documentIds)
    {
        var ids = documentIds.ToList();
        if (ids.Count == 0)
            return;

        _ = Task.Run(async () =>
        {
            foreach (var documentId in ids)
            {
                try
                {
                    await _rag.IndexDocumentAsync(documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background indexing failed for document {DocumentId}", documentId);
                }
            }
        });
    }

    private void QueueIndexing(long documentId)
    {
        QueueSequentialIndexing(new[] { documentId });
    }

    private static void NormalizeKnowledgeBaseRequest(CreateKbRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("知识库名称不能为空");

        request.Name = request.Name.Trim();
        request.Description = request.Description?.Trim();
        request.EmbeddingProvider = string.IsNullOrWhiteSpace(request.EmbeddingProvider) ? "openai" : request.EmbeddingProvider.Trim();
        request.EmbeddingModel = string.IsNullOrWhiteSpace(request.EmbeddingModel) ? "text-embedding-3-small" : request.EmbeddingModel.Trim();
        request.ChunkSize = request.ChunkSize > 0 ? request.ChunkSize : 500;
        request.ChunkOverlap = Math.Clamp(request.ChunkOverlap, 0, Math.Max(request.ChunkSize - 1, 0));
    }

    private static async Task<string> ReadUploadContentAsync(string extension, Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var raw = await reader.ReadToEndAsync();

        if (extension != ".pdf")
            return raw;

        var content = new string(raw.Where(character => character >= ' ' && character != (char)0x7F).ToArray());
        if (content.Length < 50)
            throw new InvalidOperationException("PDF 文本提取内容过少，请使用文本可复制的 PDF");

        return content;
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI.Rag;

// ── DTOs ──────────────────────────────────────────────────────────────────

public class CreateKbRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EmbeddingProvider { get; set; } = "openai";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int ChunkSize { get; set; } = 500;
    public int ChunkOverlap { get; set; } = 50;
}

public class RetrievedChunk
{
    public long ChunkId { get; set; }
    public long DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float Score { get; set; }
}

public class KbStats
{
    public long KbId { get; set; }
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public int IndexedDocumentCount { get; set; }
    public int PendingDocumentCount { get; set; }
    public int FailedDocumentCount { get; set; }
}

// ── Interface ─────────────────────────────────────────────────────────────

public interface IRagService
{
    Task<KnowledgeBase> CreateKbAsync(CreateKbRequest req);
    Task<KnowledgeBase?> GetKbAsync(long id);
    Task<List<KnowledgeBase>> GetAllKbsAsync();
    Task<KnowledgeBase> UpdateKbAsync(long id, CreateKbRequest req);
    Task DeleteKbAsync(long id);

    Task<List<KbDocument>> GetDocumentsAsync(long kbId, int page = 1, int pageSize = 20);
    Task<KbDocument> AddDocumentAsync(long kbId, string title, string content, string sourceType = "upload", long sourceId = 0);
    Task DeleteDocumentAsync(long kbId, long docId);

    /// <summary>对单个文档执行切片 + Embedding（异步后台可安全调用）</summary>
    Task IndexDocumentAsync(long docId);

    /// <summary>重建知识库内所有文档的索引</summary>
    Task ReindexAllAsync(long kbId);

    /// <summary>相似度检索，返回 top-K 切片</summary>
    Task<List<RetrievedChunk>> RetrieveAsync(long kbId, string query, int topK = 5, float vectorWeight = 0.7f, float keywordWeight = 0.3f);

    /// <summary>获取知识库统计</summary>
    Task<KbStats> GetStatsAsync(long kbId);

    /// <summary>通过纯文本内容创建文档并触发索引（用于文件上传）</summary>
    Task<KbDocument> ImportTextDocumentAsync(long kbId, string title, string content, string sourceType = "file");
}

// ── Implementation ────────────────────────────────────────────────────────

public class RagService : IRagService
{
    private readonly ISqlSugarClient _db;
    private readonly ProviderRegistry _registry;
    private readonly AiProviderSelector _selector;
    private readonly ILogger<RagService> _logger;

    public RagService(
        ISqlSugarClient db,
        ProviderRegistry registry,
        AiProviderSelector selector,
        ILogger<RagService> logger)
    {
        _db = db;
        _registry = registry;
        _selector = selector;
        _logger = logger;
    }

    // ── 知识库 CRUD ──────────────────────────────────────────────────────

    public async Task<KnowledgeBase> CreateKbAsync(CreateKbRequest req)
    {
        var kb = new KnowledgeBase
        {
            Name = req.Name,
            Description = req.Description,
            EmbeddingProvider = req.EmbeddingProvider,
            EmbeddingModel = req.EmbeddingModel,
            ChunkSize = req.ChunkSize,
            ChunkOverlap = req.ChunkOverlap,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        kb.Id = await _db.Insertable(kb).ExecuteReturnIdentityAsync();
        return kb;
    }

    public async Task<KnowledgeBase?> GetKbAsync(long id)
        => await _db.Queryable<KnowledgeBase>().FirstAsync(k => k.Id == id);

    public async Task<List<KnowledgeBase>> GetAllKbsAsync()
        => await _db.Queryable<KnowledgeBase>().OrderByDescending(k => k.CreatedAt).ToListAsync();

    public async Task<KnowledgeBase> UpdateKbAsync(long id, CreateKbRequest req)
    {
        var kb = await _db.Queryable<KnowledgeBase>().FirstAsync(k => k.Id == id)
                  ?? throw new Exception("知识库不存在");
        kb.Name = req.Name;
        kb.Description = req.Description;
        kb.EmbeddingProvider = req.EmbeddingProvider;
        kb.EmbeddingModel = req.EmbeddingModel;
        kb.ChunkSize = req.ChunkSize;
        kb.ChunkOverlap = req.ChunkOverlap;
        kb.UpdatedAt = DateTime.Now;
        await _db.Updateable(kb).ExecuteCommandAsync();
        return kb;
    }

    public async Task DeleteKbAsync(long id)
    {
        await _db.Deleteable<KbChunk>().Where(c => c.KbId == id).ExecuteCommandAsync();
        await _db.Deleteable<KbDocument>().Where(d => d.KbId == id).ExecuteCommandAsync();
        await _db.Deleteable<KnowledgeBase>().Where(k => k.Id == id).ExecuteCommandAsync();
    }

    // ── 文档管理 ─────────────────────────────────────────────────────────

    public async Task<List<KbDocument>> GetDocumentsAsync(long kbId, int page = 1, int pageSize = 20)
        => await _db.Queryable<KbDocument>()
            .Where(d => d.KbId == kbId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task<KbDocument> AddDocumentAsync(
        long kbId, string title, string content, string sourceType = "upload", long sourceId = 0)
    {
        var doc = new KbDocument
        {
            KbId = kbId,
            Title = title,
            Content = content,
            SourceType = sourceType,
            SourceId = sourceId,
            Status = "pending",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        doc.Id = await _db.Insertable(doc).ExecuteReturnIdentityAsync();
        return doc;
    }

    public async Task DeleteDocumentAsync(long kbId, long docId)
    {
        await _db.Deleteable<KbChunk>().Where(c => c.DocumentId == docId).ExecuteCommandAsync();
        await _db.Deleteable<KbDocument>().Where(d => d.Id == docId && d.KbId == kbId).ExecuteCommandAsync();
    }

    // ── 索引（切片 + Embedding）──────────────────────────────────────────

    public async Task IndexDocumentAsync(long docId)
    {
        var doc = await _db.Queryable<KbDocument>().FirstAsync(d => d.Id == docId);
        if (doc == null) return;

        var kb = await _db.Queryable<KnowledgeBase>().FirstAsync(k => k.Id == doc.KbId);
        if (kb == null) return;

        doc.Status = "indexing";
        doc.UpdatedAt = DateTime.Now;
        await _db.Updateable(doc).ExecuteCommandAsync();

        try
        {
            // 1. 删除旧切片
            await _db.Deleteable<KbChunk>().Where(c => c.DocumentId == docId).ExecuteCommandAsync();

            // 2. 切片
            var chunks = SplitText(doc.Content ?? "", kb.ChunkSize, kb.ChunkOverlap);
            if (chunks.Count == 0)
            {
                doc.Status = "indexed";
                doc.ChunkCount = 0;
                doc.UpdatedAt = DateTime.Now;
                await _db.Updateable(doc).ExecuteCommandAsync();
                return;
            }

            // 3. 尝试 Embedding（失败则降级为纯文本切片，不存向量）
            List<float[]?> vectors;
            try
            {
                vectors = await TryEmbedBatchAsync(kb, chunks);
            }
            catch (Exception embEx)
            {
                _logger.LogWarning(embEx, "Embedding failed, falling back to keyword-only mode for doc {DocId}", docId);
                vectors = chunks.Select(_ => (float[]?)null).ToList();
            }

            // 4. 入库
            var kbChunks = chunks.Select((text, idx) => new KbChunk
            {
                KbId = doc.KbId,
                DocumentId = docId,
                Content = text,
                ChunkIndex = idx,
                Vector = vectors[idx] != null ? JsonSerializer.Serialize(vectors[idx]) : null,
                TokenCount = EstimateTokens(text),
                CreatedAt = DateTime.Now
            }).ToList();

            await _db.Insertable(kbChunks).ExecuteCommandAsync();

            doc.Status = "indexed";
            doc.ChunkCount = kbChunks.Count;
            doc.UpdatedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IndexDocument failed for doc {DocId}", docId);
            doc.Status = "failed";
            doc.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 490)];
            doc.UpdatedAt = DateTime.Now;
        }

        await _db.Updateable(doc).ExecuteCommandAsync();
    }

    /// <summary>尝试批量 Embedding，不支持时抛异常由调用方决定如何处理</summary>
    private async Task<List<float[]?>> TryEmbedBatchAsync(KnowledgeBase kb, List<string> chunks)
    {
        var (embeddingProvider, apiKey, error) = await _selector.SelectAsync(
            preferredProvider: kb.EmbeddingProvider,
            type: AiProviderType.Embedding);

        if (embeddingProvider == null || apiKey == null)
            throw new Exception($"没有可用的 Provider：{error}");

        var embeddingApiUrl = _selector.GetApiUrl(kb.EmbeddingProvider);

        List<float[]> rawVectors;
        try
        {
            if (embeddingProvider is OpenAiProvider oap)
                rawVectors = await oap.EmbedBatchAsync(chunks, apiKey, kb.EmbeddingModel, apiUrl: embeddingApiUrl);
            else if (embeddingProvider is DeepSeekProvider dsp)
                rawVectors = await dsp.EmbedBatchAsync(chunks, apiKey, kb.EmbeddingModel);
            else if (embeddingProvider is IEmbeddingProvider ep)
                rawVectors = await ep.EmbedBatchAsync(chunks, apiKey, kb.EmbeddingModel);
            else
                throw new Exception($"Provider {embeddingProvider.Name} 不支持 Embedding");
        }
        catch (Exception ex)
        {
            // 调用失败（401 / 429 / 网络等）→ 标记 Key 不健康，避免后续文档继续使用坏 Key
            _selector.RecordFailure(embeddingProvider.Name, apiKey);
            _logger.LogWarning("Embedding provider {Provider} key marked unhealthy: {Error}",
                embeddingProvider.Name, ex.Message);
            throw;
        }

        _selector.RecordSuccess(embeddingProvider.Name, apiKey);
        return rawVectors.Select(v => (float[]?)v).ToList();
    }

    public async Task ReindexAllAsync(long kbId)
    {
        var docs = await _db.Queryable<KbDocument>().Where(d => d.KbId == kbId).ToListAsync();
        foreach (var doc in docs)
        {
            await IndexDocumentAsync(doc.Id);
        }
    }

    // ── 检索 ─────────────────────────────────────────────────────────────

    public async Task<List<RetrievedChunk>> RetrieveAsync(long kbId, string query, int topK = 5, float vectorWeight = 0.7f, float keywordWeight = 0.3f)
    {
        var kb = await _db.Queryable<KnowledgeBase>().FirstAsync(k => k.Id == kbId)
                  ?? throw new Exception("知识库不存在");

        var chunks = await _db.Queryable<KbChunk>()
            .Where(c => c.KbId == kbId)
            .ToListAsync();

        if (chunks.Count == 0) return new List<RetrievedChunk>();

        // 加载文档标题
        var docIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
        var docs = await _db.Queryable<KbDocument>()
            .Where(d => docIds.Contains(d.Id))
            .ToListAsync();
        var docMap = docs.ToDictionary(d => d.Id, d => d.Title);

        // 判断是否有向量：有则走向量相似度，没有则走关键词
        var chunksWithVector = chunks.Where(c => !string.IsNullOrEmpty(c.Vector)).ToList();
        if (chunksWithVector.Count > 0)
        {
            // ── 向量检索 ──
            try
            {
                var (embProvider, apiKey, error) = await _selector.SelectAsync(
                    preferredProvider: kb.EmbeddingProvider,
                    type: AiProviderType.Embedding);

                if (embProvider != null && apiKey != null)
                {
                    var embeddingApiUrl = _selector.GetApiUrl(kb.EmbeddingProvider);
                    float[] queryVec;
                    if (embProvider is OpenAiProvider oap2)
                        queryVec = await oap2.EmbedAsync(query, apiKey, kb.EmbeddingModel, apiUrl: embeddingApiUrl);
                    else if (embProvider is DeepSeekProvider dsp2)
                        queryVec = await dsp2.EmbedAsync(query, apiKey, kb.EmbeddingModel);
                    else if (embProvider is IEmbeddingProvider ep2)
                        queryVec = await ep2.EmbedAsync(query, apiKey, kb.EmbeddingModel);
                    else
                        goto keywordSearch;

                    _selector.RecordSuccess(embProvider.Name, apiKey);

                    // 向量分
                    var vectorScores = chunksWithVector
                        .Select(c =>
                        {
                            float[] vec;
                            try { vec = JsonSerializer.Deserialize<float[]>(c.Vector!)!; }
                            catch { return (chunk: c, vScore: 0f); }
                            return (chunk: c, vScore: CosineSimilarity(queryVec, vec));
                        })
                        .ToDictionary(x => x.chunk.Id, x => x.vScore);

                    // 关键词分（归一化到 0~1）
                    var kws = query.Split(' ', '，', ',', '。', '？', '?')
                        .Select(k => k.Trim()).Where(k => k.Length >= 2).ToList();
                    if (kws.Count == 0) kws.Add(query.Trim());
                    var maxKw = (float)Math.Max(kws.Count, 1);
                    var kwScores = chunksWithVector
                        .ToDictionary(c => c.Id,
                            c => kws.Sum(kw => c.Content.Contains(kw, StringComparison.OrdinalIgnoreCase) ? 1f : 0f) / maxKw);

                    // 合并分数
                    return chunksWithVector
                        .Select(c =>
                        {
                            var finalScore = vectorWeight * vectorScores.GetValueOrDefault(c.Id)
                                           + keywordWeight * kwScores.GetValueOrDefault(c.Id);
                            return (chunk: c, score: finalScore);
                        })
                        .OrderByDescending(x => x.score)
                        .Take(topK)
                        .Select(x => new RetrievedChunk
                        {
                            ChunkId       = x.chunk.Id,
                            DocumentId    = x.chunk.DocumentId,
                            DocumentTitle = docMap.GetValueOrDefault(x.chunk.DocumentId, "未知文档"),
                            Content       = x.chunk.Content,
                            Score         = x.score
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector retrieval failed, falling back to keyword search");
            }
        }

        keywordSearch:
        // ── 关键词检索（降级方案）──
        var keywords = query.Split(' ', '，', ',', '。', '？', '?')
            .Select(k => k.Trim())
            .Where(k => k.Length >= 2)
            .ToList();
        if (keywords.Count == 0) keywords.Add(query.Trim());

        return chunks
            .Select(c =>
            {
                var score = keywords.Sum(kw =>
                    c.Content.Contains(kw, StringComparison.OrdinalIgnoreCase) ? 1f : 0f);
                return (chunk: c, score);
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => new RetrievedChunk
            {
                ChunkId = x.chunk.Id,
                DocumentId = x.chunk.DocumentId,
                DocumentTitle = docMap.GetValueOrDefault(x.chunk.DocumentId, "未知文档"),
                Content = x.chunk.Content,
                Score = x.score
            })
            .ToList();
    }

    // ── 统计 ─────────────────────────────────────────────────────────────

    public async Task<KbStats> GetStatsAsync(long kbId)
    {
        var docs = await _db.Queryable<KbDocument>().Where(d => d.KbId == kbId).ToListAsync();
        var chunkCount = await _db.Queryable<KbChunk>().Where(c => c.KbId == kbId).CountAsync();
        return new KbStats
        {
            KbId = kbId,
            DocumentCount = docs.Count,
            ChunkCount = chunkCount,
            IndexedDocumentCount = docs.Count(d => d.Status == "indexed"),
            PendingDocumentCount = docs.Count(d => d.Status is "pending" or "indexing"),
            FailedDocumentCount = docs.Count(d => d.Status == "failed")
        };
    }

    public async Task<KbDocument> ImportTextDocumentAsync(long kbId, string title, string content, string sourceType = "file")
    {
        var doc = await AddDocumentAsync(kbId, title, content, sourceType);
        // 触发后台索引（不等待完成）
        _ = Task.Run(async () =>
        {
            try { await IndexDocumentAsync(doc.Id); }
            catch (Exception ex) { _logger.LogWarning(ex, "Background indexing failed for doc {DocId}", doc.Id); }
        });
        return doc;
    }

    // ── 工具方法 ─────────────────────────────────────────────────────────

    /// <summary>按段落切片，保持语义完整性</summary>
    private static List<string> SplitText(string text, int chunkSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        // 先按双换行分段
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        var chunks = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var para in paragraphs)
        {
            // 如果单段本身超过 chunkSize，强制按字符切
            if (para.Length > chunkSize * 2)
            {
                if (current.Length > 0)
                {
                    chunks.Add(current.ToString().Trim());
                    current.Clear();
                }
                for (var i = 0; i < para.Length; i += chunkSize - overlap)
                {
                    var end = Math.Min(i + chunkSize, para.Length);
                    chunks.Add(para[i..end].Trim());
                    if (end == para.Length) break;
                }
                continue;
            }

            if (current.Length + para.Length > chunkSize && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                // 保留最后 overlap 个字符作为重叠
                var overlapText = current.Length > overlap
                    ? current.ToString()[^overlap..]
                    : current.ToString();
                current.Clear();
                current.Append(overlapText);
                current.Append('\n');
            }

            current.Append(para);
            current.Append("\n\n");
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks.Where(c => c.Length >= 10).ToList();
    }

    private static int EstimateTokens(string text) => (int)(text.Length * 0.6);

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;
        var dot = 0f;
        var normA = 0f;
        var normB = 0f;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom < 1e-10f ? 0f : dot / denom;
    }
}

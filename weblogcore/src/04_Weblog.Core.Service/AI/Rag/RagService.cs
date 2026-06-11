using System.Text.Json;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI.Rag;

// 鈹€鈹€ DTOs 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

// 鈹€鈹€ Interface 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

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

    /// <summary>瀵瑰崟涓枃妗ｆ墽琛屽垏鐗?+ Embedding锛堝紓姝ュ悗鍙板彲瀹夊叏璋冪敤锛?/summary>
    Task IndexDocumentAsync(long docId);

    /// <summary>閲嶅缓鐭ヨ瘑搴撳唴鎵€鏈夋枃妗ｇ殑绱㈠紩</summary>
    Task ReindexAllAsync(long kbId);

    /// <summary>鐩镐技搴︽绱紝杩斿洖 top-K 鍒囩墖</summary>
    Task<List<RetrievedChunk>> RetrieveAsync(long kbId, string query, int topK = 5, float vectorWeight = 0.7f, float keywordWeight = 0.3f);

    /// <summary>鑾峰彇鐭ヨ瘑搴撶粺璁?/summary>
    Task<KbStats> GetStatsAsync(long kbId);

    /// <summary>閫氳繃绾枃鏈唴瀹瑰垱寤烘枃妗ｅ苟瑙﹀彂绱㈠紩锛堢敤浜庢枃浠朵笂浼狅級</summary>
    Task<KbDocument> ImportTextDocumentAsync(long kbId, string title, string content, string sourceType = "file");
}

// 鈹€鈹€ Implementation 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

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

    // 鈹€鈹€ 鐭ヨ瘑搴?CRUD 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

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
                  ?? throw new Exception("鐭ヨ瘑搴撲笉瀛樺湪");
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

    // 鈹€鈹€ 鏂囨。绠＄悊 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

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

    // 鈹€鈹€ 绱㈠紩锛堝垏鐗?+ Embedding锛夆攢鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

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
            // 1. 鍒犻櫎鏃у垏鐗?            await _db.Deleteable<KbChunk>().Where(c => c.DocumentId == docId).ExecuteCommandAsync();

            // 2. 鍒囩墖
            var chunks = RagTextProcessor.SplitText(doc.Content ?? "", kb.ChunkSize, kb.ChunkOverlap);
            if (chunks.Count == 0)
            {
                doc.Status = "indexed";
                doc.ChunkCount = 0;
                doc.UpdatedAt = DateTime.Now;
                await _db.Updateable(doc).ExecuteCommandAsync();
                return;
            }

            // 3. 灏濊瘯 Embedding锛堝け璐ュ垯闄嶇骇涓虹函鏂囨湰鍒囩墖锛屼笉瀛樺悜閲忥級
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

            // 4. 鍏ュ簱
            var kbChunks = chunks.Select((text, idx) => new KbChunk
            {
                KbId = doc.KbId,
                DocumentId = docId,
                Content = text,
                ChunkIndex = idx,
                Vector = vectors[idx] != null ? JsonSerializer.Serialize(vectors[idx]) : null,
                TokenCount = RagTextProcessor.EstimateTokens(text),
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

    /// <summary>灏濊瘯鎵归噺 Embedding锛屼笉鏀寔鏃舵姏寮傚父鐢辫皟鐢ㄦ柟鍐冲畾濡備綍澶勭悊</summary>
    private async Task<List<float[]?>> TryEmbedBatchAsync(KnowledgeBase kb, List<string> chunks)
    {
        var (embeddingProvider, apiKey, error) = await _selector.SelectAsync(
            preferredProvider: kb.EmbeddingProvider,
            type: AiProviderType.Embedding);

        if (embeddingProvider == null || apiKey == null)
            throw new Exception($"没有可用的 Embedding Provider：{error}");

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
                throw new Exception($"Provider {embeddingProvider.Name} 涓嶆敮鎸?Embedding");
        }
        catch (Exception ex)
        {
            // 璋冪敤澶辫触锛?01 / 429 / 缃戠粶绛夛級鈫?鏍囪 Key 涓嶅仴搴凤紝閬垮厤鍚庣画鏂囨。缁х画浣跨敤鍧?Key
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

    // 鈹€鈹€ 妫€绱?鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    public async Task<List<RetrievedChunk>> RetrieveAsync(long kbId, string query, int topK = 5, float vectorWeight = 0.7f, float keywordWeight = 0.3f)
    {
        var kb = await _db.Queryable<KnowledgeBase>().FirstAsync(item => item.Id == kbId)
                  ?? throw new Exception("知识库不存在");

        var chunks = await _db.Queryable<KbChunk>()
            .Where(chunk => chunk.KbId == kbId)
            .ToListAsync();

        if (chunks.Count == 0)
            return new List<RetrievedChunk>();

        var documentTitles = await GetDocumentTitleMapAsync(chunks);
        var chunksWithVector = chunks.Where(chunk => !string.IsNullOrWhiteSpace(chunk.Vector)).ToList();

        if (chunksWithVector.Count > 0)
        {
            try
            {
                var vectorResults = await TryRetrieveByVectorAsync(
                    kb, chunksWithVector, documentTitles, query, topK, vectorWeight, keywordWeight);
                if (vectorResults.Count > 0)
                    return vectorResults;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector retrieval failed, falling back to keyword search");
            }
        }

        return RetrieveByKeyword(chunks, documentTitles, query, topK);
    }

    private async Task<Dictionary<long, string>> GetDocumentTitleMapAsync(List<KbChunk> chunks)
    {
        var documentIds = chunks.Select(chunk => chunk.DocumentId).Distinct().ToList();
        var documents = await _db.Queryable<KbDocument>()
            .Where(document => documentIds.Contains(document.Id))
            .ToListAsync();

        return documents.ToDictionary(document => document.Id, document => document.Title);
    }

    private async Task<List<RetrievedChunk>> TryRetrieveByVectorAsync(
        KnowledgeBase kb,
        List<KbChunk> chunksWithVector,
        Dictionary<long, string> documentTitles,
        string query,
        int topK,
        float vectorWeight,
        float keywordWeight)
    {
        var queryVector = await EmbedQueryAsync(kb, query);
        var keywords = RagTextProcessor.ExtractKeywords(query);

        var vectorScores = chunksWithVector
            .Select(chunk =>
            {
                try
                {
                    var chunkVector = JsonSerializer.Deserialize<float[]>(chunk.Vector!);
                    var score = chunkVector == null ? 0f : RagTextProcessor.CosineSimilarity(queryVector, chunkVector);
                    return (chunk, score);
                }
                catch
                {
                    return (chunk, score: 0f);
                }
            })
            .ToDictionary(item => item.chunk.Id, item => item.score);

        var scoredChunks = chunksWithVector.Select(chunk =>
        {
            var vectorScore = vectorScores.GetValueOrDefault(chunk.Id);
            var keywordScore = RagTextProcessor.KeywordScore(chunk.Content, keywords);
            var score = vectorWeight * vectorScore + keywordWeight * keywordScore;
            return (chunk, score);
        });

        return ToRetrievedChunks(scoredChunks, documentTitles, topK);
    }

    private async Task<float[]> EmbedQueryAsync(KnowledgeBase kb, string query)
    {
        var (provider, apiKey, _) = await _selector.SelectAsync(
            preferredProvider: kb.EmbeddingProvider,
            type: AiProviderType.Embedding);

        if (provider == null || apiKey == null)
            throw new Exception("没有可用的 Embedding Provider");

        var apiUrl = _selector.GetApiUrl(kb.EmbeddingProvider);
        try
        {
            float[] queryVector = provider switch
            {
                OpenAiProvider openAi => await openAi.EmbedAsync(query, apiKey, kb.EmbeddingModel, apiUrl: apiUrl),
                DeepSeekProvider deepSeek => await deepSeek.EmbedAsync(query, apiKey, kb.EmbeddingModel),
                IEmbeddingProvider embeddingProvider => await embeddingProvider.EmbedAsync(query, apiKey, kb.EmbeddingModel),
                _ => throw new Exception($"Provider {provider.Name} 不支持 Embedding")
            };

            _selector.RecordSuccess(provider.Name, apiKey);
            return queryVector;
        }
        catch
        {
            _selector.RecordFailure(provider.Name, apiKey);
            throw;
        }
    }

    private static List<RetrievedChunk> RetrieveByKeyword(
        List<KbChunk> chunks,
        Dictionary<long, string> documentTitles,
        string query,
        int topK)
    {
        var keywords = RagTextProcessor.ExtractKeywords(query);
        var scoredChunks = chunks
            .Select(chunk => (chunk, score: RagTextProcessor.KeywordScore(chunk.Content, keywords)))
            .Where(item => item.score > 0);

        return ToRetrievedChunks(scoredChunks, documentTitles, topK);
    }

    private static List<RetrievedChunk> ToRetrievedChunks(
        IEnumerable<(KbChunk chunk, float score)> scoredChunks,
        Dictionary<long, string> documentTitles,
        int topK)
    {
        return scoredChunks
            .OrderByDescending(item => item.score)
            .Take(topK)
            .Select(item => new RetrievedChunk
            {
                ChunkId = item.chunk.Id,
                DocumentId = item.chunk.DocumentId,
                DocumentTitle = documentTitles.GetValueOrDefault(item.chunk.DocumentId, "未知文档"),
                Content = item.chunk.Content,
                Score = item.score
            })
            .ToList();
    }
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
        _ = Task.Run(async () =>
        {
            try { await IndexDocumentAsync(doc.Id); }
            catch (Exception ex) { _logger.LogWarning(ex, "Background indexing failed for doc {DocId}", doc.Id); }
        });
        return doc;
    }

}


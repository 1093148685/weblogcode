using Weblog.Core.Model.Entities;

namespace Weblog.Core.Model.DTOs;

public class CreateKbRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EmbeddingProvider { get; set; } = "openai";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int ChunkSize { get; set; } = 500;
    public int ChunkOverlap { get; set; } = 50;
}

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

public static class RagDtoMapper
{
    public static KbListItem ToKbListItem(KnowledgeBase kb) => new()
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
    };

    public static KbDocumentDto ToDocumentDto(KbDocument document) => new()
    {
        Id = document.Id,
        KbId = document.KbId,
        Title = document.Title,
        SourceType = document.SourceType,
        SourceId = document.SourceId,
        Status = document.Status,
        ErrorMessage = document.ErrorMessage,
        ChunkCount = document.ChunkCount,
        CreatedAt = document.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        UpdatedAt = document.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
    };
}

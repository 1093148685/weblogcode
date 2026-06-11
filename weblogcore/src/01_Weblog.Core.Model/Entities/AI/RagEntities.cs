using SqlSugar;

namespace Weblog.Core.Model.Entities;

/// <summary>知识库</summary>
[SugarTable("t_kb")]
public class KnowledgeBase
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>名称</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>描述</summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>使用的 Embedding Provider 名称，如 openai / zhipu</summary>
    [SugarColumn(Length = 50)]
    public string EmbeddingProvider { get; set; } = "openai";

    /// <summary>Embedding 模型，如 text-embedding-3-small</summary>
    [SugarColumn(Length = 100)]
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>每个切片的最大 token 数（约等于字符数 * 0.6）</summary>
    public int ChunkSize { get; set; } = 500;

    /// <summary>切片重叠 token 数</summary>
    public int ChunkOverlap { get; set; } = 50;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>知识库文档</summary>
[SugarTable("t_kb_document")]
public class KbDocument
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public long KbId { get; set; }

    /// <summary>文档标题</summary>
    [SugarColumn(Length = 200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>来源类型：article / wiki / upload</summary>
    [SugarColumn(Length = 20)]
    public string SourceType { get; set; } = "upload";

    /// <summary>来源 ID（文章或 Wiki ID），上传文件时为 0</summary>
    public long SourceId { get; set; } = 0;

    /// <summary>原始文本内容</summary>
    [SugarColumn(Length = -1, ColumnDataType = "longtext", IsNullable = true)]
    public string? Content { get; set; }

    /// <summary>索引状态：pending / indexing / indexed / failed</summary>
    [SugarColumn(Length = 20)]
    public string Status { get; set; } = "pending";

    /// <summary>失败原因</summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>切片数量</summary>
    public int ChunkCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>文档切片（向量块）</summary>
[SugarTable("t_kb_chunk")]
public class KbChunk
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public long KbId { get; set; }

    public long DocumentId { get; set; }

    /// <summary>切片文本内容</summary>
    [SugarColumn(Length = -1, ColumnDataType = "longtext")]
    public string Content { get; set; } = string.Empty;

    /// <summary>切片序号（文档内第几块）</summary>
    public int ChunkIndex { get; set; } = 0;

    /// <summary>向量（float[] 序列化的 JSON）</summary>
    [SugarColumn(Length = -1, ColumnDataType = "longtext", IsNullable = true)]
    public string? Vector { get; set; }

    /// <summary>token 数（估算）</summary>
    public int TokenCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

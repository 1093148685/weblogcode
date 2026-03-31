using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_ai_provider")]
public class AiProvider
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 50, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string DisplayName { get; set; } = string.Empty;

    [SugarColumn(Length = 20)]
    public string Type { get; set; } = "chat";

    [SugarColumn(Length = 500)]
    public string ApiUrl { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string EncryptedApiKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public int Priority { get; set; } = 100;

    [SugarColumn(Length = 2000, IsNullable = true)]
    public string? Config { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

[SugarTable("t_ai_plugin")]
public class AiPlugin
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 50, IsNullable = false)]
    public string PluginId { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string Description { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    [SugarColumn(Length = 2000, IsNullable = true)]
    public string? Config { get; set; }

    [SugarColumn(Length = 2000, IsNullable = true)]
    public string? Settings { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

[SugarTable("t_ai_conversation")]
public class AiConversation
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 50)]
    public string PluginId { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string SessionId { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string UserId { get; set; } = string.Empty;

    [SugarColumn(Length = -1, ColumnDataType = "longtext")]
    public string Messages { get; set; } = "[]";

    [SugarColumn(Length = 50)]
    public string ProviderName { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string Model { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

[SugarTable("t_ai_usage_log")]
public class AiUsageLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 100)]
    public string UserId { get; set; } = string.Empty;

    public int UsageCount { get; set; } = 0;

    public DateTime UsageDate { get; set; } = DateTime.Now.Date;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

[SugarTable("t_ai_agent_log")]
public class AiAgentLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 操作类型：chat, create, delete, update, toggle, approve, reject
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 操作目标：article, category, tag, comment
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// 目标 ID
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    [SugarColumn(Length = 500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 用户消息
    /// </summary>
    [SugarColumn(Length = 2000, IsNullable = true)]
    public string? UserMessage { get; set; }

    /// <summary>
    /// AI 响应
    /// </summary>
    [SugarColumn(Length = 2000, IsNullable = true)]
    public string? AiResponse { get; set; }

    /// <summary>
    /// 操作状态：success, failed
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Status { get; set; } = "success";

    /// <summary>
    /// 操作者
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Operator { get; set; } = "admin";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

[SugarTable("t_ai_agent_config")]
public class AiAgentConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 配置文件名：AGENTS, IDENTITY, SOUL, USER, MEMORY, TOOLS
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 配置文件内容
    /// </summary>
    [SugarColumn(Length = -1, ColumnDataType = "longtext")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
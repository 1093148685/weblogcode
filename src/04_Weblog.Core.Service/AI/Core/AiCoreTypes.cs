namespace Weblog.Core.Service.AI.Core;

public enum AiProviderType
{
    Chat,
    Image,
    Video,
    Audio,
    Embedding
}

public enum EncryptionType
{
    DPAPI,
    AES
}

public class AiProviderMetadata
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AiProviderType Type { get; set; }
    public List<string> Models { get; set; } = new();
    public bool SupportsStreaming { get; set; }
    public bool SupportsFunctionCalling { get; set; }
    public string? DefaultModel { get; set; }
}

public class AiToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "function";
    public AiToolCallFunction Function { get; set; } = new();
}

public class AiToolCallFunction
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

public class AiChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ToolCallId { get; set; }
    public List<AiToolCall>? ToolCalls { get; set; }
}

public class AiChatRequest
{
    public List<AiChatMessage> Messages { get; set; } = new();
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public double? MaxTokens { get; set; }
    public List<object>? Tools { get; set; }
    public string? ToolChoice { get; set; }
}

public class AgentSettings
{
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 4096;
    public int MaxTurns { get; set; } = 5;
    public string? SystemPrompt { get; set; }
    public List<string>? EnabledTools { get; set; }
}

public class AiChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int UsageInput { get; set; }
    public int UsageOutput { get; set; }
    public string? FinishReason { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
}

public class AiProviderConfig
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AiProviderType Type { get; set; }
    public string ApiUrl { get; set; } = string.Empty;
    public string EncryptedApiKey { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Config { get; set; }
}
namespace Weblog.Core.Model.DTOs;

public class AiProviderDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "chat";
    public string Protocol { get; set; } = "openai-compatible";
    public string Prefix { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Config { get; set; }
    public int ModelCount { get; set; }
    public int KeyCount { get; set; }
    public AiProviderAdvancedConfigDto ConfigData { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAiProviderRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "chat";
    public string? Protocol { get; set; }
    public string? Prefix { get; set; }
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Config { get; set; }
    public AiProviderAdvancedConfigDto? ConfigData { get; set; }
}

public class UpdateAiProviderRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "chat";
    public string? Protocol { get; set; }
    public string? Prefix { get; set; }
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Config { get; set; }
    public AiProviderAdvancedConfigDto? ConfigData { get; set; }
}

public class FetchModelsRequest
{
    public string ApiUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ModelsPath { get; set; }
    public List<AiProviderHeaderConfigDto> Headers { get; set; } = new();
}

public class AiModelOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public long Created { get; set; }
}

public class AiProviderAdvancedConfigDto
{
    public string Protocol { get; set; } = "openai-compatible";
    public string Prefix { get; set; } = string.Empty;
    public string ModelsPath { get; set; } = "/models";
    public string ChatPath { get; set; } = "/chat/completions";
    public List<AiProviderHeaderConfigDto> Headers { get; set; } = new();
    public List<AiProviderModelConfigDto> Models { get; set; } = new();
    public List<AiProviderKeyConfigDto> Keys { get; set; } = new();
}

public class AiProviderHeaderConfigDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class AiProviderModelConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; }
}

public class AiProviderKeyConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? MaskedValue { get; set; }
    public string? ProxyUrl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Status { get; set; } = "unknown";
    public DateTime? LastTestedAt { get; set; }
}

public class TestAiProviderRequest
{
    public string? Model { get; set; }
    public string? KeyId { get; set; }
}

public class ProviderHealthDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public string LastChecked { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class AiPluginDto
{
    public long Id { get; set; }
    public string PluginId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Config { get; set; }
    public string? Settings { get; set; }
}

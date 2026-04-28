namespace Weblog.Core.Model.DTOs;

public class AiProviderDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "chat";
    public string ApiUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Config { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAiProviderRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "chat";
    public string? ApiUrl { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Config { get; set; }
}

public class UpdateAiProviderRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "chat";
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public string? Config { get; set; }
}

public class FetchModelsRequest
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class AiModelOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Created { get; set; }
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

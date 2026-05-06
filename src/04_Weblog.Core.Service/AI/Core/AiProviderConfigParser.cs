using System.Text.Json;
using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.AI.Core;

public static class AiProviderConfigParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static AiProviderAdvancedConfigDto Parse(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return Normalize(new AiProviderAdvancedConfigDto());

        try
        {
            var config = JsonSerializer.Deserialize<AiProviderAdvancedConfigDto>(configJson, JsonOptions);
            if (config != null)
            {
                if ((config.Models == null || config.Models.Count == 0) && LooksLikeLegacyModelConfig(configJson))
                    return ParseLegacyConfig(configJson);
                return Normalize(config);
            }

            return Normalize(new AiProviderAdvancedConfigDto());
        }
        catch
        {
            return ParseLegacyConfig(configJson);
        }
    }

    public static string Serialize(AiProviderAdvancedConfigDto config)
    {
        return JsonSerializer.Serialize(Normalize(config), JsonOptions);
    }

    public static List<AiModelOptionDto> GetConfiguredModels(string providerName, string displayName, AiProviderAdvancedConfigDto config)
    {
        var prefix = config.Prefix?.Trim() ?? "";
        return config.Models
            .Where(m => m.IsEnabled && !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => new AiModelOptionDto
            {
                Id = BuildModelRoute(prefix, m.Id),
                Name = string.IsNullOrWhiteSpace(m.Alias)
                    ? (string.IsNullOrWhiteSpace(m.Name) ? m.Id : m.Name)
                    : m.Alias!,
                Provider = providerName,
                Created = 0
            })
            .ToList();
    }

    public static List<AiModelOptionDto> ParseModelResponse(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
            return new List<AiModelOptionDto>();

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            return ParseModelArray(root);

        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return ParseModelArray(data);

        if (root.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
            return ParseModelArray(models);

        return new List<AiModelOptionDto>();
    }

    public static AiModelRoute ParseModelRoute(string? model)
    {
        var raw = model?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(raw))
            return new AiModelRoute(null, "");

        var slashIndex = raw.IndexOf('/');
        if (slashIndex > 0 && slashIndex < raw.Length - 1)
            return new AiModelRoute(raw[..slashIndex], raw[(slashIndex + 1)..]);

        return new AiModelRoute(GuessProviderFromModel(raw), raw);
    }

    public static string BuildModelRoute(string? prefix, string modelId)
    {
        var cleanModel = modelId.Trim();
        var cleanPrefix = prefix?.Trim().Trim('/') ?? "";
        return string.IsNullOrWhiteSpace(cleanPrefix) ? cleanModel : $"{cleanPrefix}/{cleanModel}";
    }

    public static string ResolveModelForProvider(AiProviderConfig provider, string? requestedModel)
    {
        var raw = requestedModel?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var advanced = Parse(provider.Config);
        var configuredModels = advanced.Models
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .ToList();

        var exactConfiguredModel = configuredModels.FirstOrDefault(m =>
            m.Id.Equals(raw, StringComparison.OrdinalIgnoreCase));
        if (exactConfiguredModel != null)
            return exactConfiguredModel.Id;

        var prefix = string.IsNullOrWhiteSpace(provider.Prefix) ? advanced.Prefix : provider.Prefix;
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var prefixedModel = configuredModels.FirstOrDefault(m =>
                BuildModelRoute(prefix, m.Id).Equals(raw, StringComparison.OrdinalIgnoreCase));
            if (prefixedModel != null)
                return prefixedModel.Id;
        }

        var slashIndex = raw.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= raw.Length - 1)
            return raw;

        var routePrefix = raw[..slashIndex];
        var routedModel = raw[(slashIndex + 1)..];
        var providerIds = new[]
            {
                provider.Name,
                prefix,
                provider.DisplayName
            }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim());

        return providerIds.Contains(routePrefix, StringComparer.OrdinalIgnoreCase)
            ? routedModel
            : raw;
    }

    public static AiModelRoute ResolveRequestRoute(AiProviderConfig provider, string? requestedModel, string? explicitProvider = null)
    {
        var explicitHint = explicitProvider?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitHint))
            return new AiModelRoute(explicitHint, ResolveModelForProvider(provider, requestedModel));

        var parsed = ParseModelRoute(requestedModel);
        return new AiModelRoute(parsed.ProviderHint, ResolveModelForProvider(provider, requestedModel));
    }

    public static string MaskKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        var clean = key.Trim();
        if (clean.Length <= 10)
            return "****";

        return $"{clean[..4]}****{clean[^4..]}";
    }

    private static AiProviderAdvancedConfigDto Normalize(AiProviderAdvancedConfigDto config)
    {
        config.Protocol = string.IsNullOrWhiteSpace(config.Protocol)
            ? "openai-compatible"
            : config.Protocol.Trim().ToLowerInvariant();
        config.Prefix = config.Prefix?.Trim().Trim('/') ?? "";
        config.ModelsPath = NormalizePath(config.ModelsPath, "/models");
        config.ChatPath = NormalizePath(config.ChatPath, "/chat/completions");
        config.Headers ??= new List<AiProviderHeaderConfigDto>();
        config.Models ??= new List<AiProviderModelConfigDto>();
        config.Keys ??= new List<AiProviderKeyConfigDto>();

        foreach (var key in config.Keys.Where(k => string.IsNullOrWhiteSpace(k.Id)))
            key.Id = Guid.NewGuid().ToString("N");

        foreach (var model in config.Models)
        {
            model.Id = model.Id?.Trim() ?? "";
            model.Name = string.IsNullOrWhiteSpace(model.Name) ? model.Id : model.Name.Trim();
            model.Alias = string.IsNullOrWhiteSpace(model.Alias) ? null : model.Alias.Trim();
        }

        return config;
    }

    private static string NormalizePath(string? path, string fallback)
    {
        if (string.IsNullOrWhiteSpace(path))
            return fallback;

        var trimmed = path.Trim();
        return trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
    }

    private static AiProviderAdvancedConfigDto ParseLegacyConfig(string configJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (doc.RootElement.TryGetProperty("model", out var modelProp))
            {
                var model = modelProp.GetString();
                if (!string.IsNullOrWhiteSpace(model))
                {
                    return Normalize(new AiProviderAdvancedConfigDto
                    {
                        Models = new List<AiProviderModelConfigDto>
                        {
                            new() { Id = model!, Name = model!, IsEnabled = true, IsDefault = true }
                        }
                    });
                }
            }
        }
        catch
        {
        }

        return Normalize(new AiProviderAdvancedConfigDto());
    }

    private static bool LooksLikeLegacyModelConfig(string configJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("model", out _);
        }
        catch
        {
            return false;
        }
    }

    private static List<AiModelOptionDto> ParseModelArray(JsonElement array)
    {
        var result = new List<AiModelOptionDto>();
        foreach (var item in array.EnumerateArray())
        {
            var id = ReadModelId(item);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var name = ReadString(item, "name")
                ?? ReadString(item, "displayName")
                ?? ReadString(item, "display_name")
                ?? id;

            result.Add(new AiModelOptionDto
            {
                Id = id,
                Name = name,
                Created = ReadInt64(item, "created") ?? 0
            });
        }

        return result
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static string ReadModelId(JsonElement item)
    {
        var id = ReadString(item, "id")
            ?? ReadString(item, "model")
            ?? ReadString(item, "name");

        if (id?.StartsWith("models/", StringComparison.OrdinalIgnoreCase) == true)
            id = id["models/".Length..];

        return id ?? "";
    }

    private static string? ReadString(JsonElement item, string property)
    {
        return item.ValueKind == JsonValueKind.Object
            && item.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    private static long? ReadInt64(JsonElement item, string property)
    {
        if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(property, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            return number;

        return null;
    }

    private static string? GuessProviderFromModel(string model)
    {
        var m = model.ToLowerInvariant();
        if (m.StartsWith("deepseek")) return "deepseek";
        if (m.StartsWith("gpt-") || m.StartsWith("o1") || m.StartsWith("o3") || m.StartsWith("o4")) return "openai";
        if (m.StartsWith("claude")) return "claude";
        if (m.StartsWith("gemini")) return "gemini";
        if (m.StartsWith("glm")) return "zhipu";
        if (m.StartsWith("ernie") || m == "qianfan") return "qianfan";
        if (m.StartsWith("minimax")) return "minimax";
        return null;
    }
}

public record AiModelRoute(string? ProviderHint, string ModelId);

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Weblog.Core.Service.AI.WebSearch;

public interface IMcpSearchService
{
    Task<List<WebSearchResult>> SearchAsync(string query, int topK, CancellationToken ct = default);
}

public class McpSearchSettings
{
    public bool Enabled { get; set; }
    public bool PreferMcp { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 20;
    public List<McpSearchServerConfig> Servers { get; set; } = new();
}

public class McpSearchServerConfig
{
    public string Name { get; set; } = "mcp";
    public bool Enabled { get; set; } = true;
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string> Env { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? WorkingDirectory { get; set; }
    public string? ToolName { get; set; }
    public string QueryArgument { get; set; } = "query";
    public string MaxResultsArgument { get; set; } = "max_results";
    public int TimeoutSeconds { get; set; }
    public Dictionary<string, object?> Arguments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class McpSearchArgumentBuilder
{
    public static Dictionary<string, object?> Build(McpSearchServerConfig server, string query, int topK)
    {
        var arguments = new Dictionary<string, object?>(server.Arguments, StringComparer.OrdinalIgnoreCase)
        {
            [string.IsNullOrWhiteSpace(server.QueryArgument) ? "query" : server.QueryArgument] = query
        };

        if (!string.IsNullOrWhiteSpace(server.MaxResultsArgument))
        {
            arguments[server.MaxResultsArgument] = topK;
        }

        return arguments;
    }
}

public static class McpSearchResultParser
{
    public static List<WebSearchResult> ParseToolCallResult(string serverName, string payload, int topK)
    {
        if (string.IsNullOrWhiteSpace(payload)) return new List<WebSearchResult>();

        try
        {
            using var document = JsonDocument.Parse(payload);
            return Reindex(ParseJsonElement(serverName, document.RootElement, topK));
        }
        catch (JsonException)
        {
            return Reindex(ParsePlainText(serverName, payload, topK));
        }
    }

    private static IEnumerable<WebSearchResult> ParseJsonElement(string serverName, JsonElement element, int topK)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryGetArray(element, out var array))
            {
                foreach (var item in ParseArray(serverName, array, topK))
                {
                    yield return item;
                }
                yield break;
            }

            if (element.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    foreach (var item in ParseContentBlock(serverName, block, topK))
                    {
                        yield return item;
                    }
                }
                yield break;
            }

            if (TryMapObject(serverName, element, out var single))
            {
                yield return single;
            }

            yield break;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in ParseArray(serverName, element, topK))
            {
                yield return item;
            }
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            foreach (var item in ParseText(serverName, element.GetString() ?? string.Empty, topK))
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<WebSearchResult> ParseContentBlock(string serverName, JsonElement block, int topK)
    {
        if (block.ValueKind != JsonValueKind.Object)
        {
            foreach (var item in ParseJsonElement(serverName, block, topK))
            {
                yield return item;
            }
            yield break;
        }

        if (block.TryGetProperty("json", out var json))
        {
            foreach (var item in ParseJsonElement(serverName, json, topK))
            {
                yield return item;
            }
        }

        if (block.TryGetProperty("data", out var data))
        {
            foreach (var item in ParseJsonElement(serverName, data, topK))
            {
                yield return item;
            }
        }

        var text = FirstString(block, "text", "content", "markdown");
        if (!string.IsNullOrWhiteSpace(text))
        {
            foreach (var item in ParseText(serverName, text, topK))
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<WebSearchResult> ParseText(string serverName, string text, int topK)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                return ParseJsonElement(serverName, document.RootElement, topK).ToList();
            }
            catch (JsonException)
            {
                // Fall through to URL extraction.
            }
        }

        return ParsePlainText(serverName, trimmed, topK);
    }

    private static IEnumerable<WebSearchResult> ParseArray(string serverName, JsonElement array, int topK)
    {
        foreach (var item in array.EnumerateArray().Take(Math.Max(1, topK)))
        {
            if (TryMapObject(serverName, item, out var result))
            {
                yield return result;
            }
            else
            {
                foreach (var nested in ParseJsonElement(serverName, item, topK))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool TryGetArray(JsonElement element, out JsonElement array)
    {
        foreach (var name in new[] { "results", "items", "data", "documents", "sources" })
        {
            if (element.TryGetProperty(name, out array) && array.ValueKind == JsonValueKind.Array)
            {
                return true;
            }
        }

        array = default;
        return false;
    }

    private static bool TryMapObject(string serverName, JsonElement item, out WebSearchResult result)
    {
        result = new WebSearchResult();
        if (item.ValueKind != JsonValueKind.Object) return false;

        var url = FirstString(item, "url", "link", "href", "source", "uri");
        if (string.IsNullOrWhiteSpace(url)) return false;

        result = new WebSearchResult
        {
            Title = FirstString(item, "title", "name", "heading") ?? url,
            Url = url,
            Snippet = FirstString(item, "content", "snippet", "description", "text", "summary") ?? string.Empty,
            SourceType = $"mcp:{serverName}",
            Relevance = FirstFloat(item, "score", "relevance", "rank"),
            ContentQuality = 0.35f,
            HasUsableContent = true
        };
        return true;
    }

    private static IEnumerable<WebSearchResult> ParsePlainText(string serverName, string text, int topK)
    {
        var matches = Regex.Matches(text, @"https?://[^\s\]\)""']+", RegexOptions.IgnoreCase);
        foreach (Match match in matches.Take(Math.Max(1, topK)))
        {
            var url = match.Value.TrimEnd('.', ',', ';');
            var line = text.Split('\n')
                .Select(item => item.Trim())
                .FirstOrDefault(item => item.Contains(url, StringComparison.OrdinalIgnoreCase));
            var title = ExtractMarkdownTitle(line, url);
            yield return new WebSearchResult
            {
                Title = string.IsNullOrWhiteSpace(title) ? url : title,
                Url = url,
                Snippet = line ?? text,
                SourceType = $"mcp:{serverName}",
                Relevance = 0.5f,
                ContentQuality = 0.3f,
                HasUsableContent = true
            };
        }
    }

    private static string ExtractMarkdownTitle(string? line, string url)
    {
        if (string.IsNullOrWhiteSpace(line)) return string.Empty;
        var markdown = Regex.Match(line, @"\[(?<title>[^\]]+)\]\(" + Regex.Escape(url) + @"\)");
        if (markdown.Success) return markdown.Groups["title"].Value.Trim();
        return line.Replace(url, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim(' ', '-', '*', ':', '|');
    }

    private static List<WebSearchResult> Reindex(IEnumerable<WebSearchResult> results)
    {
        return results
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
            .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select((item, index) =>
            {
                item.Index = index + 1;
                return item;
            })
            .ToList();
    }

    private static string? FirstString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.String) return property.GetString();
                if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                {
                    return property.ToString();
                }
            }
        }

        return null;
    }

    private static float FirstFloat(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number && property.TryGetSingle(out var value))
                {
                    return Math.Clamp(value, 0f, 1f);
                }

                if (property.ValueKind == JsonValueKind.String
                    && float.TryParse(property.GetString(), out var parsed))
                {
                    return Math.Clamp(parsed, 0f, 1f);
                }
            }
        }

        return 0.5f;
    }
}

public class McpSearchService : IMcpSearchService
{
    private const string ProtocolVersion = "2025-06-18";

    private readonly IConfiguration _configuration;
    private readonly ILogger<McpSearchService> _logger;

    public McpSearchService(IConfiguration configuration, ILogger<McpSearchService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<WebSearchResult>> SearchAsync(string query, int topK, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<WebSearchResult>();

        var settings = LoadSettings();
        if (!settings.Enabled) return new List<WebSearchResult>();

        var results = new List<WebSearchResult>();
        foreach (var server in settings.Servers.Where(IsRunnableServer))
        {
            var timeout = TimeSpan.FromSeconds(server.TimeoutSeconds > 0 ? server.TimeoutSeconds : settings.TimeoutSeconds);
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(timeout);

                var serverResults = await SearchServerAsync(server, query, topK, timeoutCts.Token);
                results.AddRange(serverResults);

                if (results.Count >= topK) break;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("MCP search server {Server} timed out after {TimeoutSeconds}s", server.Name, timeout.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP search server {Server} failed", server.Name);
            }
        }

        return results
            .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(Math.Max(1, topK))
            .Select((item, index) =>
            {
                item.Index = index + 1;
                return item;
            })
            .ToList();
    }

    private async Task<List<WebSearchResult>> SearchServerAsync(McpSearchServerConfig server, string query, int topK, CancellationToken ct)
    {
        using var process = StartProcess(server);

        var writer = process.StandardInput;
        var reader = process.StandardOutput;

        await SendRequestAsync(writer, 1, "initialize", new
        {
            protocolVersion = ProtocolVersion,
            capabilities = new { },
            clientInfo = new { name = "weblog-core", version = "1.0.0" }
        }, ct);
        await ReadResponseAsync(reader, 1, ct);

        await SendNotificationAsync(writer, "notifications/initialized", new { }, ct);

        await SendRequestAsync(writer, 2, "tools/list", new { }, ct);
        var toolsResponse = await ReadResponseAsync(reader, 2, ct);
        var toolName = ResolveToolName(server, toolsResponse);
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return new List<WebSearchResult>();
        }

        await SendRequestAsync(writer, 3, "tools/call", new
        {
            name = toolName,
            arguments = McpSearchArgumentBuilder.Build(server, query, topK)
        }, ct);
        var callResponse = await ReadResponseAsync(reader, 3, ct);

        if (!callResponse.TryGetProperty("result", out var result))
        {
            return new List<WebSearchResult>();
        }

        var parsed = McpSearchResultParser.ParseToolCallResult(server.Name, result.GetRawText(), topK);
        return parsed;
    }

    private Process StartProcess(McpSearchServerConfig server)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = server.Command,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(server.WorkingDirectory))
        {
            startInfo.WorkingDirectory = server.WorkingDirectory;
        }

        foreach (var arg in server.Args)
        {
            if (!string.IsNullOrWhiteSpace(arg)) startInfo.ArgumentList.Add(arg);
        }

        foreach (var item in server.Env)
        {
            startInfo.Environment[item.Key] = ExpandEnv(item.Value);
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start MCP server {server.Name}");
        }

        _ = Task.Run(async () =>
        {
            try
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _logger.LogDebug("MCP {Server} stderr: {Line}", server.Name, line);
                    }
                }
            }
            catch
            {
                // Ignore stderr reader shutdown races.
            }
        });

        return process;
    }

    private static async Task SendRequestAsync(StreamWriter writer, int id, string method, object parameters, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params = parameters
        });
        await writer.WriteLineAsync(payload.AsMemory(), ct);
        await writer.FlushAsync(ct);
    }

    private static async Task SendNotificationAsync(StreamWriter writer, string method, object parameters, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            method,
            @params = parameters
        });
        await writer.WriteLineAsync(payload.AsMemory(), ct);
        await writer.FlushAsync(ct);
    }

    private static async Task<JsonElement> ReadResponseAsync(StreamReader reader, int id, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null)
            {
                throw new InvalidOperationException("MCP server closed stdout before responding");
            }

            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id)
                {
                    continue;
                }

                if (root.TryGetProperty("error", out var error))
                {
                    throw new InvalidOperationException($"MCP request {id} failed: {error}");
                }

                return root.Clone();
            }
        }

        throw new OperationCanceledException(ct);
    }

    private static string ResolveToolName(McpSearchServerConfig server, JsonElement toolsResponse)
    {
        if (!string.IsNullOrWhiteSpace(server.ToolName)) return server.ToolName;

        if (!toolsResponse.TryGetProperty("result", out var result)
            || !result.TryGetProperty("tools", out var tools)
            || tools.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var searchTools = tools.EnumerateArray()
            .Select(tool => new
            {
                Name = GetString(tool, "name"),
                Description = GetString(tool, "description")
            })
            .Where(tool => !string.IsNullOrWhiteSpace(tool.Name))
            .ToList();

        var preferred = searchTools.FirstOrDefault(tool =>
            ContainsSearchHint(tool.Name) || ContainsSearchHint(tool.Description));

        return preferred?.Name ?? searchTools.FirstOrDefault()?.Name ?? string.Empty;
    }

    private McpSearchSettings LoadSettings()
    {
        var section = _configuration.GetSection("Mcp:Search");
        var settings = new McpSearchSettings
        {
            Enabled = GetBool(section, "Enabled", false),
            PreferMcp = GetBool(section, "PreferMcp", true),
            TimeoutSeconds = Math.Clamp(GetInt(section, "TimeoutSeconds", 20), 3, 120)
        };

        foreach (var serverSection in section.GetSection("Servers").GetChildren())
        {
            var server = new McpSearchServerConfig
            {
                Name = serverSection["Name"] ?? serverSection.Key,
                Enabled = GetBool(serverSection, "Enabled", true),
                Command = serverSection["Command"] ?? string.Empty,
                WorkingDirectory = serverSection["WorkingDirectory"],
                ToolName = serverSection["ToolName"],
                QueryArgument = serverSection["QueryArgument"] ?? "query",
                MaxResultsArgument = serverSection["MaxResultsArgument"] ?? "max_results",
                TimeoutSeconds = GetInt(serverSection, "TimeoutSeconds", 0)
            };

            server.Args = serverSection.GetSection("Args").GetChildren()
                .Select(item => item.Value ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();

            foreach (var env in serverSection.GetSection("Env").GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(env.Key))
                {
                    server.Env[env.Key] = env.Value ?? string.Empty;
                }
            }

            foreach (var argument in serverSection.GetSection("Arguments").GetChildren())
            {
                server.Arguments[argument.Key] = ParseConfigValue(argument.Value);
            }

            settings.Servers.Add(server);
        }

        return settings;
    }

    private static bool IsRunnableServer(McpSearchServerConfig server)
    {
        return server.Enabled && !string.IsNullOrWhiteSpace(server.Command);
    }

    private static bool ContainsSearchHint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Regex.IsMatch(value, "search|web|brave|tavily|google|bing|searx|fetch", RegexOptions.IgnoreCase);
    }

    private static object? ParseConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (bool.TryParse(value, out var boolean)) return boolean;
        if (int.TryParse(value, out var integer)) return integer;
        if (float.TryParse(value, out var number)) return number;
        return ExpandEnv(value);
    }

    private static string ExpandEnv(string value)
    {
        var expanded = Environment.ExpandEnvironmentVariables(value);
        return Regex.Replace(expanded, @"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}", match =>
            Environment.GetEnvironmentVariable(match.Groups["name"].Value) ?? match.Value);
    }

    private static bool GetBool(IConfiguration section, string key, bool defaultValue)
    {
        return bool.TryParse(section[key], out var value) ? value : defaultValue;
    }

    private static int GetInt(IConfiguration section, string key, int defaultValue)
    {
        return int.TryParse(section[key], out var value) ? value : defaultValue;
    }

    private static string GetString(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
               && element.TryGetProperty(name, out var property)
               && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }
}

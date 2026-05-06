using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI;
using Weblog.Core.Service.AI.WebSearch;

public static class Program
{
    public static int Main()
    {
        var tests = new List<(string Name, Action Run)>
        {
            ("empty config uses safe defaults", EmptyConfigUsesSafeDefaults),
            ("provider config parses headers models keys", ProviderConfigParsesHeadersModelsKeys),
            ("configured models expose prefixed selectable ids", ConfiguredModelsExposePrefixedSelectableIds),
            ("model response parser normalizes OpenAI compatible payload", ModelResponseParserNormalizesOpenAiPayload),
            ("model route helper splits provider prefix from model id", ModelRouteHelperSplitsProviderPrefix),
            ("provider model resolver preserves slashful model ids", ProviderModelResolverPreservesSlashfulModelIds),
            ("provider model resolver strips configured route prefixes", ProviderModelResolverStripsConfiguredRoutePrefixes),
            ("request route resolver keeps slashful model id when provider is explicit", RequestRouteResolverKeepsSlashfulModelIdWhenProviderIsExplicit),
            ("chat error sanitizer maps unauthorized to friendly rate message", ChatErrorSanitizerMapsUnauthorizedToFriendlyRateMessage),
            ("chat error sanitizer keeps explicit frequent message", ChatErrorSanitizerKeepsExplicitFrequentMessage),
            ("legacy AiModel fallback is scoped to matching provider", LegacyAiModelFallbackIsScopedToMatchingProvider),
            ("MCP search parser reads JSON text content", McpSearchParserReadsJsonTextContent),
            ("MCP search arguments honor configured names", McpSearchArgumentsHonorConfiguredNames)
        };

        var passed = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
                return 1;
            }
        }

        Console.WriteLine($"{passed}/{tests.Count} tests passed");
        return 0;
    }

    private static void EmptyConfigUsesSafeDefaults()
    {
        var config = AiProviderConfigParser.Parse(null);

        AssertEqual("openai-compatible", config.Protocol);
        AssertEqual("/models", config.ModelsPath);
        AssertEqual("/chat/completions", config.ChatPath);
        AssertEqual(0, config.Headers.Count);
        AssertEqual(0, config.Models.Count);
        AssertEqual(0, config.Keys.Count);
    }

    private static void ProviderConfigParsesHeadersModelsKeys()
    {
        var json = """
        {
          "protocol": "openai-compatible",
          "prefix": "team-a",
          "modelsPath": "/v1/models",
          "chatPath": "/v1/chat/completions",
          "headers": [
            { "name": "X-Team", "value": "blue", "enabled": true }
          ],
          "models": [
            { "id": "gpt-4o-mini", "name": "Fast GPT", "alias": "fast", "isEnabled": true, "isDefault": true }
          ],
          "keys": [
            { "id": "k1", "value": "dummy-key-live", "proxyUrl": "socks5://127.0.0.1:7890", "isEnabled": true }
          ]
        }
        """;

        var config = AiProviderConfigParser.Parse(json);

        AssertEqual("openai-compatible", config.Protocol);
        AssertEqual("team-a", config.Prefix);
        AssertEqual("/v1/models", config.ModelsPath);
        AssertEqual("/v1/chat/completions", config.ChatPath);
        AssertEqual("X-Team", config.Headers.Single().Name);
        AssertEqual("Fast GPT", config.Models.Single().Name);
        AssertEqual("fast", config.Models.Single().Alias);
        AssertEqual("socks5://127.0.0.1:7890", config.Keys.Single().ProxyUrl);
    }

    private static void ConfiguredModelsExposePrefixedSelectableIds()
    {
        var config = new AiProviderAdvancedConfigDto
        {
            Prefix = "team-a",
            Models =
            [
                new AiProviderModelConfigDto { Id = "gpt-4o-mini", Name = "Fast GPT", IsEnabled = true },
                new AiProviderModelConfigDto { Id = "disabled-model", Name = "Disabled", IsEnabled = false }
            ]
        };

        var models = AiProviderConfigParser.GetConfiguredModels("openai", "OpenAI", config);

        AssertEqual(1, models.Count);
        AssertEqual("team-a/gpt-4o-mini", models[0].Id);
        AssertEqual("Fast GPT", models[0].Name);
        AssertEqual("openai", models[0].Provider);
    }

    private static void ModelResponseParserNormalizesOpenAiPayload()
    {
        const string payload = """
        {
          "object": "list",
          "data": [
            { "id": "gpt-4o-mini", "created": 1715367049 },
            { "id": "custom-model", "name": "Custom Model" }
          ]
        }
        """;

        var models = AiProviderConfigParser.ParseModelResponse(payload);

        AssertEqual(2, models.Count);
        AssertEqual("gpt-4o-mini", models[0].Id);
        AssertEqual("gpt-4o-mini", models[0].Name);
        AssertEqual(1715367049L, models[0].Created);
        AssertEqual("custom-model", models[1].Id);
        AssertEqual("Custom Model", models[1].Name);
    }

    private static void ModelRouteHelperSplitsProviderPrefix()
    {
        var route = AiProviderConfigParser.ParseModelRoute("team-a/gpt-4o-mini");

        AssertEqual("team-a", route.ProviderHint);
        AssertEqual("gpt-4o-mini", route.ModelId);
    }

    private static void ProviderModelResolverPreservesSlashfulModelIds()
    {
        var provider = new AiProviderConfig
        {
            Name = "openrouter",
            DisplayName = "OpenRouter",
            Config = AiProviderConfigParser.Serialize(new AiProviderAdvancedConfigDto
            {
                Models =
                [
                    new AiProviderModelConfigDto { Id = "openai/gpt-4o-mini", Name = "GPT via OpenRouter", IsEnabled = true }
                ]
            })
        };

        var model = AiProviderConfigParser.ResolveModelForProvider(provider, "openai/gpt-4o-mini");

        AssertEqual("openai/gpt-4o-mini", model);
    }

    private static void ProviderModelResolverStripsConfiguredRoutePrefixes()
    {
        var provider = new AiProviderConfig
        {
            Name = "xiaomi",
            DisplayName = "Xiaomi",
            Config = AiProviderConfigParser.Serialize(new AiProviderAdvancedConfigDto
            {
                Prefix = "mi",
                Models =
                [
                    new AiProviderModelConfigDto { Id = "MiMo-7B-RL", Name = "MiMo", IsEnabled = true }
                ]
            })
        };

        AssertEqual("MiMo-7B-RL", AiProviderConfigParser.ResolveModelForProvider(provider, "mi/MiMo-7B-RL"));
        AssertEqual("MiMo-7B-RL", AiProviderConfigParser.ResolveModelForProvider(provider, "xiaomi/MiMo-7B-RL"));
    }

    private static void RequestRouteResolverKeepsSlashfulModelIdWhenProviderIsExplicit()
    {
        var provider = new AiProviderConfig
        {
            Name = "openrouter",
            DisplayName = "OpenRouter",
            Config = AiProviderConfigParser.Serialize(new AiProviderAdvancedConfigDto
            {
                Models =
                [
                    new AiProviderModelConfigDto { Id = "openai/gpt-4o-mini", Name = "GPT via OpenRouter", IsEnabled = true }
                ]
            })
        };

        var route = AiProviderConfigParser.ResolveRequestRoute(provider, "openai/gpt-4o-mini", "openrouter");

        AssertEqual("openrouter", route.ProviderHint);
        AssertEqual("openai/gpt-4o-mini", route.ModelId);
    }

    private static void ChatErrorSanitizerMapsUnauthorizedToFriendlyRateMessage()
    {
        var ex = new HttpRequestException("Response status code does not indicate success: 401 (Unauthorized).");

        var message = AiChatErrorSanitizer.ToUserMessage(ex);

        AssertEqual("请求过于频繁，请稍后再试", message);
    }

    private static void ChatErrorSanitizerKeepsExplicitFrequentMessage()
    {
        var message = AiChatErrorSanitizer.ToUserMessage("请求频繁，请稍后再试");

        AssertEqual("请求过于频繁，请稍后再试", message);
    }

    private static void LegacyAiModelFallbackIsScopedToMatchingProvider()
    {
        var provider = new AiProviderConfig
        {
            Name = "deepseek",
            DisplayName = "DeepSeek",
            Prefix = "ds"
        };

        var unrelatedLegacyModel = new AiModel
        {
            Type = "openai",
            Model = "gpt-4o-mini",
            ApiKey = "dummy-key-openai"
        };

        var matchingLegacyModel = new AiModel
        {
            Type = "deepseek",
            Model = "deepseek-chat",
            ApiKey = "dummy-key-deepseek"
        };

        var prefixedLegacyModel = new AiModel
        {
            Type = "ds",
            Model = "deepseek-chat",
            ApiKey = "dummy-key-deepseek"
        };

        AssertFalse(AiProviderSelector.IsLegacyModelForProvider(unrelatedLegacyModel, provider));
        AssertTrue(AiProviderSelector.IsLegacyModelForProvider(matchingLegacyModel, provider));
        AssertTrue(AiProviderSelector.IsLegacyModelForProvider(prefixedLegacyModel, provider));
    }

    private static void McpSearchParserReadsJsonTextContent()
    {
        const string payload = """
        {
          "content": [
            {
              "type": "text",
              "text": "{\"results\":[{\"title\":\"MCP spec\",\"url\":\"https://modelcontextprotocol.io/specification\",\"content\":\"Model Context Protocol tools use JSON-RPC.\"}]}"
            }
          ]
        }
        """;

        var results = McpSearchResultParser.ParseToolCallResult("brave", payload, 3);

        AssertEqual(1, results.Count);
        AssertEqual("MCP spec", results[0].Title);
        AssertEqual("https://modelcontextprotocol.io/specification", results[0].Url);
        AssertEqual("mcp:brave", results[0].SourceType);
        AssertTrue(results[0].Snippet.Contains("JSON-RPC", StringComparison.OrdinalIgnoreCase));
    }

    private static void McpSearchArgumentsHonorConfiguredNames()
    {
        var server = new McpSearchServerConfig
        {
            Name = "custom-search",
            QueryArgument = "q",
            MaxResultsArgument = "limit"
        };

        var args = McpSearchArgumentBuilder.Build(server, "latest MCP SDK", 4);

        AssertEqual("latest MCP SDK", args["q"]);
        AssertEqual(4, args["limit"]);
        AssertFalse(args.ContainsKey("query"));
        AssertFalse(args.ContainsKey("max_results"));
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"expected {expected}, got {actual}");
        }
    }

    private static void AssertTrue(bool value)
    {
        if (!value)
            throw new InvalidOperationException("expected true, got false");
    }

    private static void AssertFalse(bool value)
    {
        if (value)
            throw new InvalidOperationException("expected false, got true");
    }
}

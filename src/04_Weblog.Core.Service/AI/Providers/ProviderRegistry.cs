using Microsoft.Extensions.DependencyInjection;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;

namespace Weblog.Core.Service.AI.Providers;

public class ProviderRegistry
{
    private readonly Dictionary<string, Func<IAiProvider>> _providers = new();
    private readonly IServiceProvider _serviceProvider;

    public ProviderRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Register<TProvider>() where TProvider : IAiProvider
    {
        var provider = Activator.CreateInstance<TProvider>();
        _providers[provider.Name.ToLower()] = () => provider;
    }

    public void Register(string name, Func<IAiProvider> factory)
    {
        _providers[name.ToLower()] = factory;
    }

    public IAiProvider? Get(string name)
    {
        if (_providers.TryGetValue(name.ToLower(), out var factory))
        {
            return factory();
        }
        return null;
    }

    public IEnumerable<AiProviderMetadata> GetAllMetadata()
    {
        var result = new List<AiProviderMetadata>();
        foreach (var factory in _providers)
        {
            var provider = factory.Value();
            result.Add(new AiProviderMetadata
            {
                Name = provider.Name,
                DisplayName = provider.DisplayName,
                Type = provider.Type,
                Models = provider.Models,
                SupportsStreaming = provider.SupportsStreaming,
                SupportsFunctionCalling = provider.SupportsFunctionCalling,
                DefaultModel = provider.DefaultModel
            });
        }
        return result;
    }

    public bool IsSupported(string name)
    {
        return _providers.ContainsKey(name.ToLower());
    }

    public static void RegisterDefaultProviders(ProviderRegistry registry)
    {
        registry.Register<OpenAiProvider>();
        registry.Register<ClaudeProvider>();
        registry.Register<DeepSeekProvider>();
        registry.Register<AzureOpenAiProvider>();
        registry.Register<GeminiProvider>();
        registry.Register<ZhipuProvider>();
        registry.Register<QianfanProvider>();
        registry.Register<MiniMaxProvider>();
    }
}

public static class ProviderRegistryExtensions
{
    public static IServiceCollection AddAiProviders(this IServiceCollection services)
    {
        services.AddSingleton<ProviderRegistry>(sp =>
        {
            var registry = new ProviderRegistry(sp);
            ProviderRegistry.RegisterDefaultProviders(registry);
            return registry;
        });
        return services;
    }
}
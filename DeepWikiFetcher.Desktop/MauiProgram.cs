using System.Reflection;
using DeepWikiFetcher.Desktop.ViewModels;
using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Services.Services;
using DeepWikiFetcher.Shared.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Desktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        ConfigureServices(builder.Services);

        var app = builder.Build();
        App.Services = app.Services;
        return app;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.Configure<CrawlerOptions>(options =>
        {
            options.MaxConcurrency = 3;
            options.ChannelCapacity = 100;
        });
        services.Configure<PlaywrightOptions>(options => options.Enabled = false);
        services.Configure<TranslationOptions>(options =>
        {
            options.BatchSize = 10;
            options.MaxConcurrency = 1;
            options.CacheExpirationDays = 30;
            options.RequestDelayMs = 1000;
        });

        services.AddHttpClient();
        services.AddHttpClient<IPageFetcher, PageFetcher>();
        services.AddServicesFromAssembly(typeof(IUrlTransformer).Assembly);
        services.AddServicesFromAssembly(typeof(ICacheManager).Assembly);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutputGenerator, MarkdownWriter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutputGenerator, StaticSiteGenerator>());
        services.AddSingleton<OutputSerializer>();

        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<CrawlViewModel>();
        services.AddSingleton<HistoryViewModel>();
    }

    private static IServiceCollection AddServicesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var allTypes = assembly.GetExportedTypes();
        var interfaces = allTypes.Where(type => type.IsInterface && type.Name.StartsWith("I", StringComparison.Ordinal)).ToList();
        var implementations = allTypes.Where(type => type.IsClass && !type.IsAbstract).ToList();

        foreach (var interfaceType in interfaces)
        {
            string expectedName = interfaceType.Name[1..];
            var implementationType = implementations.FirstOrDefault(type =>
                type.Name == expectedName && interfaceType.IsAssignableFrom(type));

            if (implementationType is not null)
            {
                services.TryAddSingleton(interfaceType, implementationType);
            }
        }

        return services;
    }
}

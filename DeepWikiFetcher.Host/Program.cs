using DeepWikiFetcher.Host;
using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Services.Services;
using DeepWikiFetcher.Shared.Enums;
using DeepWikiFetcher.Shared.Models;
using DeepWikiFetcher.Shared.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;

// 命令行参数解析
string? url = null;
string output = "./Output";
bool serverMode = false;
bool translate = false;
var outputFormat = OutputFormat.Markdown;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] is "--url" or "-u" && i + 1 < args.Length)
    {
        url = args[++i];
    }
    else if (args[i] is "--output" or "-o" && i + 1 < args.Length)
    {
        output = args[++i];
    }
    else if (args[i] is "--server" or "-s")
    {
        serverMode = true;
    }
    else if (args[i] is "--translate" or "-t")
    {
        translate = true;
    }
    else if (args[i] is "--format" or "-f" && i + 1 < args.Length)
    {
        if (!Enum.TryParse(args[++i], ignoreCase: true, out outputFormat))
        {
            throw new ArgumentException("Unsupported output format. Use Markdown or StaticSite.");
        }
    }
}

if (!serverMode && string.IsNullOrWhiteSpace(url))
{
    // 默认无参数 → 启动 Server 模式
    serverMode = true;
}

if (serverMode)
{
    // ========== Server 模式 ==========
    await RunServerMode(args, output);
}
else
{
    // ========== CLI 模式 ==========
    await RunCliMode(url!, output, outputFormat, translate, args);
}

// ===== Server 模式实现 =====
static async Task RunServerMode(string[] args, string output)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddOpenApi();

    ConfigureServices(builder.Services, builder.Configuration);

    var app = builder.Build();

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "DeepWikiFetcher API";
        options.Theme = ScalarTheme.Purple;
    });

    // 健康检查
    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
        .WithTags("System");

    // URL 转换
    app.MapGet("/transform", (string url, IUrlTransformer transformer) =>
        Results.Ok(new { Source = url, Target = transformer.Transform(url) }))
        .WithTags("Pipeline");

    // 侧边栏解析
    app.MapGet("/sidebar", async (string url, ISidebarParser parser, CancellationToken ct) =>
        Results.Ok(await parser.ParseAsync(url, ct)))
        .WithTags("Pipeline");

    // 单页获取
    app.MapGet("/page", async (string url, IPageFetcher fetcher, CancellationToken ct) =>
    {
        var html = await fetcher.FetchAsync(url, ct);
        return Results.Ok(new { Url = url, Length = html.Length });
    }).WithTags("Pipeline");

    // HTML 清洗
    app.MapPost("/clean", async (CleanRequest req, IHtmlCleaner cleaner, CancellationToken ct) =>
    {
        var result = await cleaner.CleanAsync(req.Html, req.BaseUrl ?? req.Url, ct);
        return Results.Ok(new { result.CleanHtml, result.ImageUrls });
    }).WithTags("Pipeline");

    // 完整爬取
    app.MapPost("/crawl", async (CrawlRequest request, ICrawlOrchestrator orchestrator) =>
    {
        var options = new CrawlOptions
        {
            GitHubUrl = request.Url,
            OutputRoot = output,
            OutputFormat = OutputFormat.Markdown,
            TranslationEnabled = false
        };
        var result = await orchestrator.StartAsync(options);
        return Results.Ok(result);
    }).WithTags("Pipeline");

    Console.WriteLine("DeepWikiFetcher Server ready");
    app.Run();
}

// ===== CLI 模式实现 =====
static async Task RunCliMode(string url, string output, OutputFormat outputFormat, bool translate, string[] args)
{
    Console.WriteLine("DeepWikiFetcher ready");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.template.json", optional: true, reloadOnChange: false);

    ConfigureServices(builder.Services, builder.Configuration, translate);

    var host = builder.Build();
    var orchestrator = host.Services.GetRequiredService<ICrawlOrchestrator>();

    var crawlOptions = new CrawlOptions
    {
        GitHubUrl = url,
        OutputRoot = output,
        OutputFormat = outputFormat,
        TranslationEnabled = translate || builder.Configuration.GetValue<bool>($"{TranslationOptions.SectionName}:Enabled")
    };

    var result = await orchestrator.StartAsync(crawlOptions);

    Console.WriteLine();
    Console.WriteLine("=== Crawl Complete ===");
    Console.WriteLine($"Repository:    {result.RepoKey}");
    Console.WriteLine($"Total Pages:   {result.TotalPages}");
    Console.WriteLine($"Success:       {result.SuccessCount}");
    Console.WriteLine($"Failed:        {result.FailCount}");
    Console.WriteLine($"Duration:      {result.Duration}");
    Console.WriteLine($"Output:        {result.OutputPath}");
}

// ===== 共用 DI 注册 =====
static void ConfigureServices(IServiceCollection services, IConfiguration configuration, bool forceTranslation = false)
{
    services.Configure<CrawlerOptions>(configuration.GetSection("Crawler"));
    services.Configure<PlaywrightOptions>(configuration.GetSection("Playwright"));
    services.AddOptions<TranslationOptions>()
        .Bind(configuration.GetSection(TranslationOptions.SectionName))
        .Validate(options => options.BatchSize >= 1 && options.BatchSize <= 100, "Translation:BatchSize must be between 1 and 100.")
        .Validate(options => options.MaxConcurrency >= 1 && options.MaxConcurrency <= 10, "Translation:MaxConcurrency must be between 1 and 10.")
        .Validate(options => options.CacheExpirationDays >= 1, "Translation:CacheExpirationDays must be greater than zero.")
        .Validate(options => options.RequestDelayMs >= 0, "Translation:RequestDelayMs must be greater than or equal to zero.")
        .ValidateOnStart();

    if (forceTranslation)
    {
        services.PostConfigure<TranslationOptions>(options => options.Enabled = true);
    }

    // HttpClient（命名注册，需手动指定）
    services.AddHttpClient();
    services.AddHttpClient<IPageFetcher, DeepWikiFetcher.Services.Services.PageFetcher>();

    // 自动扫描注册：IXxx → Xxx (Singleton)
    services.AddServicesFromAssembly(typeof(DeepWikiFetcher.Services.Interfaces.IUrlTransformer).Assembly);
    services.AddServicesFromAssembly(typeof(DeepWikiFetcher.Infrastructure.Interfaces.ICacheManager).Assembly);

    // 命名不匹配约定的手动注册
    services.AddOutputGenerators();
    services.AddSingleton<OutputSerializer>();

    services.AddLogging(logging => logging.AddConsole());
}

// 爬取 API 请求模型
public record CrawlRequest(string Url);

// 清洗 API 请求模型
public record CleanRequest(string Html, string Url, string? BaseUrl);

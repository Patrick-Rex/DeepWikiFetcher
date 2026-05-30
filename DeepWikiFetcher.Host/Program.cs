using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Services.Services;
using DeepWikiFetcher.Shared.Enums;
using DeepWikiFetcher.Shared.Models;
using DeepWikiFetcher.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("DeepWikiFetcher ready");

// 手动解析命令行参数：dotnet run -- --url <github-url> --output <path>
string? url = null;
string output = "./Output";

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
}

if (string.IsNullOrWhiteSpace(url))
{
    Console.Error.WriteLine("Error: --url <github-url> is required.");
    Console.Error.WriteLine("Usage: dotnet run -- --url <github-url> [--output <path>]");
    return 1;
}

var builder = Host.CreateApplicationBuilder(args);

// 加载 appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.template.json", optional: true, reloadOnChange: false);

// IOptions<T> 绑定
builder.Services.Configure<CrawlerOptions>(
    builder.Configuration.GetSection("Crawler"));
builder.Services.Configure<PlaywrightOptions>(
    builder.Configuration.GetSection("Playwright"));
builder.Services.Configure<TranslationOptions>(
    builder.Configuration.GetSection("Translation"));

// HttpClient
builder.Services.AddHttpClient<IPageFetcher, DeepWikiFetcher.Services.Services.PageFetcher>();

// Service 接口注册
builder.Services.AddSingleton<IUrlTransformer, DeepWikiFetcher.Services.Services.UrlTransformer>();
builder.Services.AddSingleton<ISidebarParser, DeepWikiFetcher.Services.Services.SidebarParser>();
builder.Services.AddSingleton<IHtmlCleaner, DeepWikiFetcher.Services.Services.HtmlCleaner>();
builder.Services.AddSingleton<IOutputGenerator, DeepWikiFetcher.Services.Services.MarkdownWriter>();
builder.Services.AddSingleton<ICrawlOrchestrator, DeepWikiFetcher.Services.Services.CrawlOrchestrator>();
builder.Services.AddSingleton<OutputSerializer>();

// Infrastructure 接口注册
builder.Services.AddSingleton<ICacheManager, DeepWikiFetcher.Infrastructure.Services.CacheManager>();
builder.Services.AddSingleton<IPollyPipeline, DeepWikiFetcher.Infrastructure.Services.PollyPipeline>();

// 日志
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});

var host = builder.Build();

var orchestrator = host.Services.GetRequiredService<ICrawlOrchestrator>();
var crawlOptions = new CrawlOptions
{
    GitHubUrl = url,
    OutputRoot = output,
    OutputFormat = OutputFormat.Markdown,
    TranslationEnabled = false
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

return 0;
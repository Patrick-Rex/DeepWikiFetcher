using System.Diagnostics;
using System.Threading.Channels;
using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Models;
using DeepWikiFetcher.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 爬取编排器：使用 Channel&lt;T&gt; 生产者-消费者模型 + SemaphoreSlim 控制并发，
/// 协调 UrlTransformer → SidebarParser → [PageFetcher → HtmlCleaner per page] → MarkdownWriter 流水线。
/// </summary>
public sealed class CrawlOrchestrator : ICrawlOrchestrator
{
    private readonly IUrlTransformer _urlTransformer;
    private readonly ISidebarParser _sidebarParser;
    private readonly IPageFetcher _pageFetcher;
    private readonly IHtmlCleaner _htmlCleaner;
    private readonly IAssetDownloader _assetDownloader;
    private readonly ITranslationService _translationService;
    private readonly IEnumerable<IOutputGenerator> _outputGenerators;
    private readonly OutputSerializer _outputSerializer;
    private readonly ICacheManager _cacheManager;
    private readonly CrawlerOptions _crawlerOptions;
    private readonly ILogger<CrawlOrchestrator> _logger;

    public CrawlOrchestrator(
        IUrlTransformer urlTransformer,
        ISidebarParser sidebarParser,
        IPageFetcher pageFetcher,
        IHtmlCleaner htmlCleaner,
        IAssetDownloader assetDownloader,
        ITranslationService translationService,
        IEnumerable<IOutputGenerator> outputGenerators,
        OutputSerializer outputSerializer,
        ICacheManager cacheManager,
        IOptions<CrawlerOptions> crawlerOptions,
        ILogger<CrawlOrchestrator> logger)
    {
        _urlTransformer = urlTransformer;
        _sidebarParser = sidebarParser;
        _pageFetcher = pageFetcher;
        _htmlCleaner = htmlCleaner;
        _assetDownloader = assetDownloader;
        _translationService = translationService;
        _outputGenerators = outputGenerators;
        _outputSerializer = outputSerializer;
        _cacheManager = cacheManager;
        _crawlerOptions = crawlerOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CrawlResult> StartAsync(
        CrawlOptions options,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Crawl start: {GitHubUrl}", options.GitHubUrl);
        progress?.Report(new CrawlProgress
        {
            Phase = Shared.Enums.CrawlPhase.UrlTransform,
            LogMessage = "Crawl started"
        });

        // 1. URL 转换
        var deepWikiUrl = _urlTransformer.Transform(options.GitHubUrl);
        _logger.LogInformation("DeepWiki URL: {Url}", deepWikiUrl);
        progress?.Report(new CrawlProgress
        {
            Phase = Shared.Enums.CrawlPhase.SidebarParse,
            LogMessage = $"DeepWiki URL: {deepWikiUrl}"
        });

        // 2. 侧边栏解析 → 获取 DocumentNode 树
        var docTree = await _sidebarParser.ParseAsync(deepWikiUrl, ct);

        // 3. 计算输出目录
        var repoKey = GetRepoKey(options.GitHubUrl);
        var outputDir = Path.Combine(options.OutputRoot, repoKey);
        Directory.CreateDirectory(outputDir);

        // 保存爬取开始元数据
        await _cacheManager.SaveMetadataAsync(new CrawlMetadata
        {
            RepoKey = repoKey,
            StartedAt = DateTime.UtcNow,
            Status = "Running",
            TotalPages = 0,
            SuccessPages = 0,
            FailedPages = 0
        });

        // 4. 收集所有需要爬取的页面节点
        var pages = new List<DocumentNode>();
        CollectPages(docTree, pages);
        var totalPages = pages.Count;
        _logger.LogInformation("Total pages to fetch: {Count}", totalPages);
        progress?.Report(new CrawlProgress
        {
            Phase = Shared.Enums.CrawlPhase.PageFetch,
            TotalPages = totalPages,
            CompletedPages = 0,
            LogMessage = $"Total pages: {totalPages}"
        });

        // 5. 生产者-消费者：通过 Channel 分发页面
        var channelOptions = new BoundedChannelOptions(_crawlerOptions.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        var channel = Channel.CreateBounded<DocumentNode>(channelOptions);
        var semaphore = new SemaphoreSlim(_crawlerOptions.MaxConcurrency);

        var successCount = 0;
        var failCount = 0;
        var completedCount = 0;
        var lockObj = new object();

        // 消费者任务
        var consumerTasks = new List<Task>();
        for (int i = 0; i < _crawlerOptions.MaxConcurrency; i++)
        {
            consumerTasks.Add(Task.Run(async () =>
            {
                await foreach (var node in channel.Reader.ReadAllAsync(ct))
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        await ProcessPageAsync(node, outputDir, ct);
                        lock (lockObj) { successCount++; }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Page fetch failed (degraded): {Url}", node.Url);
                        lock (lockObj) { failCount++; }
                    }
                    finally
                    {
                        int completed = Interlocked.Increment(ref completedCount);
                        progress?.Report(new CrawlProgress
                        {
                            Phase = Shared.Enums.CrawlPhase.PageFetch,
                            TotalPages = totalPages,
                            CompletedPages = completed,
                            CurrentPageTitle = node.Title,
                            LogMessage = $"Processed {node.Title}"
                        });
                        semaphore.Release();
                    }
                }
            }, ct));
        }

        // 生产者：将页面节点写入 Channel
        await Task.Run(async () =>
        {
            foreach (var page in pages)
            {
                await channel.Writer.WriteAsync(page, ct);
            }
            channel.Writer.Complete();
        }, ct);

        // 等待所有消费者完成
        await Task.WhenAll(consumerTasks);

        // 6. 可选翻译
        if (options.TranslationEnabled)
        {
            await _translationService.TranslateBatchAsync(docTree, progress, ct);
        }

        // 7. 输出生成
        progress?.Report(new CrawlProgress
        {
            Phase = Shared.Enums.CrawlPhase.Output,
            TotalPages = totalPages,
            CompletedPages = totalPages,
            LogMessage = $"Generating {options.OutputFormat} output"
        });
        var outputGenerator = _outputGenerators.FirstOrDefault(generator => generator.Format == options.OutputFormat)
            ?? throw new InvalidOperationException($"Output generator not registered: {options.OutputFormat}");
        await outputGenerator.GenerateAsync(docTree, outputDir, ct);
        await _outputSerializer.WriteMetadataAsync(new CrawlResult
        {
            RepoKey = repoKey,
            TotalPages = totalPages,
            SuccessCount = successCount,
            FailCount = failCount,
            Duration = sw.Elapsed,
            OutputPath = outputDir
        }, outputDir, ct);
        await _outputSerializer.WriteIndexAsync(docTree, outputDir, ct);

        sw.Stop();

        var result = new CrawlResult
        {
            RepoKey = repoKey,
            TotalPages = totalPages,
            SuccessCount = successCount,
            FailCount = failCount,
            Duration = sw.Elapsed,
            OutputPath = outputDir
        };

        // 保存爬取完成元数据
        await _cacheManager.SaveMetadataAsync(new CrawlMetadata
        {
            RepoKey = repoKey,
            StartedAt = DateTime.UtcNow.Subtract(sw.Elapsed),
            CompletedAt = DateTime.UtcNow,
            Status = "Completed",
            TotalPages = totalPages,
            SuccessPages = successCount,
            FailedPages = failCount
        });

        _logger.LogInformation(
            "Crawl complete: {RepoKey} — {Success}/{Total} pages, {Fail} failed, {Duration}",
            result.RepoKey, result.SuccessCount, result.TotalPages, result.FailCount, result.Duration);

        return result;
    }

    private async Task ProcessPageAsync(DocumentNode node, string outputDir, CancellationToken ct)
    {
        _logger.LogInformation("Processing page ({Number}): {Url}", node.Number, node.Url);

        var rawHtml = await _pageFetcher.FetchAsync(node.Url, ct);
        var cleanResult = await _htmlCleaner.CleanAsync(rawHtml, node.Url, ct);
        cleanResult.AssetInfos = await _assetDownloader.DownloadAsync(cleanResult.ImageUrls, outputDir, ct);

        foreach (var assetInfo in cleanResult.AssetInfos.Where(asset => asset.Downloaded))
        {
            cleanResult.CleanHtml = cleanResult.CleanHtml.Replace(
                assetInfo.OriginalUrl,
                $"assets/images/{assetInfo.LocalFileName}",
                StringComparison.OrdinalIgnoreCase);
        }

        // 将清洗后的 HTML 暂存到 node（用于 MarkdownWriter 输出）
        // DocumentNode 模型需添加 Content 字段（在 MarkdownWriter 中直接引用）
        node.Content = cleanResult.CleanHtml;
        _logger.LogInformation("Page processed ({Number}): {Url}", node.Number, node.Url);
    }

    private static void CollectPages(DocumentNode node, List<DocumentNode> pages)
    {
        if (node.Depth > 0 && !string.IsNullOrEmpty(node.Url))
        {
            pages.Add(node);
        }
        foreach (var child in node.Children)
        {
            CollectPages(child, pages);
        }
    }

    private static string GetRepoKey(string githubUrl)
    {
        var uri = new Uri(githubUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        return segments.Length >= 2 ? $"{segments[0]}/{segments[1]}" : "unknown";
    }
}

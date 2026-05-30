using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 页面获取器：HttpClient 优先下载，SQLite 缓存加速，Playwright 配置开关兜底（默认禁用）。
/// Polly 弹性管道处理限流/重试/熔断。
/// </summary>
public sealed class PageFetcher : IPageFetcher
{
    private readonly HttpClient _httpClient;
    private readonly PlaywrightOptions _playwrightOptions;
    private readonly ICacheManager _cacheManager;
    private readonly IPollyPipeline _pollyPipeline;
    private readonly CrawlerOptions _crawlerOptions;
    private readonly ILogger<PageFetcher> _logger;

    public PageFetcher(
        HttpClient httpClient,
        IOptions<PlaywrightOptions> playwrightOptions,
        IOptions<CrawlerOptions> crawlerOptions,
        ICacheManager cacheManager,
        IPollyPipeline pollyPipeline,
        ILogger<PageFetcher> logger)
    {
        _httpClient = httpClient;
        _playwrightOptions = playwrightOptions.Value;
        _crawlerOptions = crawlerOptions.Value;
        _cacheManager = cacheManager;
        _pollyPipeline = pollyPipeline;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> FetchAsync(string pageUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching page: {Url}", pageUrl);

        // 1. 检查缓存
        var cached = await _cacheManager.GetPageAsync(pageUrl);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT: {Url}", pageUrl);
            return cached;
        }

        // 2. 优先尝试 HTTP（带 Polly 弹性管道）
        try
        {
            var pipeline = _pollyPipeline.CreatePipeline(_crawlerOptions);

            var response = await pipeline.ExecuteAsync(
                async innerCt => await _httpClient.GetAsync(pageUrl, innerCt),
                ct);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(ct);

            // 3. 写入缓存
            await _cacheManager.SetPageAsync(pageUrl, content);

            _logger.LogInformation("HTTP fetch success: {Url}", pageUrl);
            return content;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "HTTP fetch failed (after retries): {Url}", pageUrl);

            // 4. HTTP 失败，尝试 Playwright 兜底
            if (_playwrightOptions.Enabled)
            {
                return await FetchWithPlaywrightAsync(pageUrl, ct);
            }

            throw;
        }
    }

    private Task<string> FetchWithPlaywrightAsync(string pageUrl, CancellationToken ct)
    {
        _logger.LogInformation("Playwright fallback requested for: {Url}", pageUrl);
        throw new NotSupportedException(
            "Playwright mode is enabled but not yet implemented. " +
            "Install Playwright runtime or disable the Playwright option in appsettings.json.");
    }
}

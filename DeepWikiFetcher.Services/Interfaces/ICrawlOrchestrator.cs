using DeepWikiFetcher.Shared.Models;

namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 爬取编排器：协调整个爬取流水线的执行。
/// </summary>
public interface ICrawlOrchestrator
{
    /// <summary>
    /// 启动爬取流水线。
    /// </summary>
    /// <param name="options">爬取配置</param>
    /// <param name="progress">进度报告回调</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>爬取结果汇总</returns>
    Task<CrawlResult> StartAsync(
        CrawlOptions options,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken ct = default);
}

using DeepWikiFetcher.Shared.Options;
using Polly;

namespace DeepWikiFetcher.Infrastructure.Interfaces;

/// <summary>
/// Polly 弹性管道工厂：创建限流 + 重试 + 熔断组合策略。
/// </summary>
public interface IPollyPipeline
{
    /// <summary>
    /// 创建针对 HTTP 请求的弹性管道。
    /// </summary>
    /// <param name="options">爬虫配置选项</param>
    /// <returns>组合弹性策略</returns>
    IAsyncPolicy<HttpResponseMessage> CreatePipeline(CrawlerOptions options);
}

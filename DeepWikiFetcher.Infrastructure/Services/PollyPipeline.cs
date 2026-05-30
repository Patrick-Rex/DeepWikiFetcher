using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Shared.Options;
using Polly;
using Polly.CircuitBreaker;
using System.Net;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Infrastructure.Services;

/// <summary>
/// 使用 Polly 构建弹性管道：重试（指数退避）+ 熔断。降级由 CrawlOrchestrator try-catch 处理。
/// </summary>
public sealed class PollyPipeline : IPollyPipeline
{
    private readonly ILogger<PollyPipeline> _logger;

    public PollyPipeline(ILogger<PollyPipeline> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IAsyncPolicy<HttpResponseMessage> CreatePipeline(CrawlerOptions options)
    {
        // 1. 重试策略：指数退避，处理 429/5xx/HttpRequestException
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r =>
                (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: options.MaxRetryCount,
                sleepDurationProvider: attempt =>
                {
                    var delay = TimeSpan.FromSeconds(options.BaseDelaySeconds * Math.Pow(2, attempt - 1));
                    return delay;
                },
                onRetryAsync: (outcome, delay, attempt, ctx) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "exception";
                    _logger.LogWarning(
                        "Retry #{Attempt} after {Delay}s for status={Status}",
                        attempt, delay.TotalSeconds, statusCode);
                    return Task.CompletedTask;
                });

        // 2. 熔断策略
        var circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .AdvancedCircuitBreakerAsync(
                failureThreshold: options.CircuitBreakerThreshold / 100.0,
                samplingDuration: TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                minimumThroughput: 3,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                onBreak: (outcome, delay) =>
                {
                    _logger.LogError("Circuit breaker OPEN for {Duration}s", delay.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker RESET");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker HALF-OPEN — probing");
                });

        // 3. 组合策略：重试 → 熔断
        // 降级（单页失败不中断）由 CrawlOrchestrator try-catch 处理

        return Policy.WrapAsync(retryPolicy, circuitBreaker);
    }
}

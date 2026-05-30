namespace DeepWikiFetcher.Shared.Options;

/// <summary>
/// 爬虫配置选项，通过 IOptions&lt;CrawlerOptions&gt; 从 appsettings.json 绑定。
/// </summary>
public sealed class CrawlerOptions
{
    /// <summary>每分钟最大请求数</summary>
    public int RateLimitPerMinute { get; set; } = 30;

    /// <summary>最小请求间隔（毫秒）</summary>
    public int MinIntervalMs { get; set; } = 2000;

    /// <summary>最大重试次数</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>重试基础延迟（秒）</summary>
    public int BaseDelaySeconds { get; set; } = 1;

    /// <summary>熔断器失败阈值</summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>熔断持续时间（秒）</summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>最大并发爬取数</summary>
    public int MaxConcurrency { get; set; } = 3;

    /// <summary>Channel 有界容量</summary>
    public int ChannelCapacity { get; set; } = 100;

    /// <summary>HTTP 请求超时（秒）</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>缓存过期时间（小时）</summary>
    public int CacheExpirationHours { get; set; } = 24;

    /// <summary>输出格式</summary>
    public string OutputFormat { get; set; } = "Markdown";
}

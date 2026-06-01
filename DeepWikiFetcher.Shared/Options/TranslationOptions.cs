namespace DeepWikiFetcher.Shared.Options;

/// <summary>
/// 翻译配置选项，通过 IOptions&lt;TranslationOptions&gt; 从 appsettings.json 绑定。
/// </summary>
public sealed class TranslationOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "Translation";

    /// <summary>是否启用翻译</summary>
    public bool Enabled { get; set; }

    /// <summary>OpenAI 兼容 API 地址</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API 密钥</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>翻译模型名称</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>翻译最大并发数。</summary>
    public int MaxConcurrency { get; set; } = 1;

    /// <summary>翻译批大小。</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>缓存过期天数。</summary>
    public int CacheExpirationDays { get; set; } = 30;

    /// <summary>翻译请求间隔毫秒数。</summary>
    public int RequestDelayMs { get; set; } = 1000;
}

namespace DeepWikiFetcher.Shared.Options;

/// <summary>
/// 翻译配置选项（v1 占位），通过 IOptions&lt;TranslationOptions&gt; 从 appsettings.json 绑定。
/// </summary>
public sealed class TranslationOptions
{
    /// <summary>是否启用翻译</summary>
    public bool Enabled { get; set; }

    /// <summary>OpenAI 兼容 API 地址</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API 密钥</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>翻译模型名称</summary>
    public string Model { get; set; } = string.Empty;
}

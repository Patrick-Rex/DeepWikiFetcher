namespace DeepWikiFetcher.Shared.Options;

/// <summary>
/// Playwright 浏览器渲染选项，通过 IOptions&lt;PlaywrightOptions&gt; 从 appsettings.json 绑定。
/// </summary>
public sealed class PlaywrightOptions
{
    /// <summary>是否启用 Playwright 兜底模式</summary>
    public bool Enabled { get; set; }

    /// <summary>浏览器可执行文件路径（空字符串表示自动检测）</summary>
    public string BrowserPath { get; set; } = string.Empty;

    /// <summary>是否使用无头模式</summary>
    public bool Headless { get; set; } = true;

    /// <summary>页面加载超时（秒）</summary>
    public int PageLoadTimeoutSeconds { get; set; } = 60;
}

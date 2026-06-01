namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 静态站点配置。
/// </summary>
public sealed class StaticSiteConfig
{
    /// <summary>站点标题。</summary>
    public string SiteTitle { get; set; } = string.Empty;

    /// <summary>默认语言。</summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>可用语言列表。</summary>
    public List<string> AvailableLanguages { get; set; } = [];

    /// <summary>仓库标识。</summary>
    public string RepoKey { get; set; } = string.Empty;
}
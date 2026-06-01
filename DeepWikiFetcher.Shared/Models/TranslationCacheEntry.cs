namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 翻译缓存记录，用于映射 SQLite translation_cache 表。
/// </summary>
public sealed class TranslationCacheEntry
{
    /// <summary>原文内容 SHA256 哈希。</summary>
    public string SourceHash { get; set; } = string.Empty;

    /// <summary>页面 URL。</summary>
    public string PageUrl { get; set; } = string.Empty;

    /// <summary>原始 Markdown 正文。</summary>
    public string SourceText { get; set; } = string.Empty;

    /// <summary>翻译后的 Markdown 正文。</summary>
    public string TranslatedText { get; set; } = string.Empty;

    /// <summary>翻译模型标识。</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>缓存创建时间。</summary>
    public DateTimeOffset CachedAt { get; set; }

    /// <summary>缓存过期时间。</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
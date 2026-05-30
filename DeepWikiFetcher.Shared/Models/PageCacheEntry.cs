namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 页面缓存记录，存储于 SQLite page_cache 表。
/// </summary>
public sealed record PageCacheEntry
{
    /// <summary>URL 的 SHA256 哈希（主键）</summary>
    public string UrlHash { get; init; } = string.Empty;

    /// <summary>原始 URL</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>缓存的原始 HTML 内容</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>缓存时间</summary>
    public DateTime CachedAt { get; init; }

    /// <summary>过期时间</summary>
    public DateTime ExpiresAt { get; init; }
}

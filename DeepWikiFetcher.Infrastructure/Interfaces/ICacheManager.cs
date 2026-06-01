using DeepWikiFetcher.Shared.Models;

namespace DeepWikiFetcher.Infrastructure.Interfaces;

/// <summary>
/// SQLite 缓存管理器，管理页面缓存和爬取元数据。
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// 从缓存中获取页面内容（检查过期时间）。
    /// </summary>
    /// <param name="url">页面 URL</param>
    /// <returns>缓存的 HTML 内容，缓存未命中或过期返回 null</returns>
    Task<string?> GetPageAsync(string url);

    /// <summary>
    /// 将页面内容存入缓存。
    /// </summary>
    /// <param name="url">页面 URL</param>
    /// <param name="content">HTML 内容</param>
    Task SetPageAsync(string url, string content);

    /// <summary>
    /// 保存爬取元数据。
    /// </summary>
    /// <param name="metadata">爬取元数据</param>
    Task SaveMetadataAsync(CrawlMetadata metadata);

    /// <summary>
    /// 获取指定仓库的爬取元数据。
    /// </summary>
    /// <param name="repoKey">仓库标识（owner/repo）</param>
    /// <returns>爬取元数据，不存在返回 null</returns>
    Task<CrawlMetadata?> GetMetadataAsync(string repoKey);

    /// <summary>
    /// 获取翻译缓存。
    /// </summary>
    /// <param name="sourceText">原始正文。</param>
    /// <param name="model">翻译模型。</param>
    /// <returns>缓存条目，未命中或过期返回 null。</returns>
    Task<TranslationCacheEntry?> GetTranslationAsync(string sourceText, string model);

    /// <summary>
    /// 保存翻译缓存。
    /// </summary>
    /// <param name="entry">翻译缓存条目。</param>
    Task SetTranslationAsync(TranslationCacheEntry entry);
}

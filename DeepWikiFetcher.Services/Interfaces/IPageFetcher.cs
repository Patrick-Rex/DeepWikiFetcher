namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 下载 DeepWiki 页面内容，支持 HTTP 优先 + Playwright 兜底双模式。
/// </summary>
public interface IPageFetcher
{
    /// <summary>
    /// 获取指定 URL 的页面 HTML 内容。
    /// </summary>
    /// <param name="pageUrl">页面 URL</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>页面 HTML 字符串</returns>
    Task<string> FetchAsync(string pageUrl, CancellationToken ct = default);
}

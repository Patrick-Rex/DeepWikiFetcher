using DeepWikiFetcher.Shared.Models;

namespace DeepWikiFetcher.Infrastructure.Interfaces;

/// <summary>
/// 静态资源下载器接口。
/// </summary>
public interface IAssetDownloader
{
    /// <summary>
    /// 批量下载图片资源。
    /// </summary>
    /// <param name="imageUrls">图片 URL 列表。</param>
    /// <param name="outputRoot">输出根目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下载结果列表。</returns>
    Task<List<AssetInfo>> DownloadAsync(
        List<string> imageUrls,
        string outputRoot,
        CancellationToken cancellationToken = default);
}
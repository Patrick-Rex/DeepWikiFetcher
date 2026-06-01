namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 静态资源下载结果。
/// </summary>
public sealed class AssetInfo
{
    /// <summary>原始资源 URL。</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>本地文件名。</summary>
    public string LocalFileName { get; set; } = string.Empty;

    /// <summary>是否下载成功。</summary>
    public bool Downloaded { get; set; }
}
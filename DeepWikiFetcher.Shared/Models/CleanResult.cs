namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// HTML 清洗结果，包含清洗后的 HTML 和提取的图片引用。
/// </summary>
public sealed class CleanResult
{
    /// <summary>清洗后的 HTML 字符串</summary>
    public string CleanHtml { get; set; } = string.Empty;

    /// <summary>提取的图片引用列表（绝对 URL）</summary>
    public List<string> ImageUrls { get; set; } = [];

    /// <summary>下载完成的图片资源列表。</summary>
    public List<AssetInfo> AssetInfos { get; set; } = [];
}

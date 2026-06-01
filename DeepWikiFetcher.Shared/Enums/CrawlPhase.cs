namespace DeepWikiFetcher.Shared.Enums;

/// <summary>
/// 爬取流水线阶段。
/// </summary>
public enum CrawlPhase
{
    /// <summary>URL 转换阶段。</summary>
    UrlTransform,

    /// <summary>侧边栏解析阶段。</summary>
    SidebarParse,

    /// <summary>页面获取阶段。</summary>
    PageFetch,

    /// <summary>HTML 清洗阶段。</summary>
    HtmlClean,

    /// <summary>资源下载阶段。</summary>
    AssetDownload,

    /// <summary>翻译阶段。</summary>
    Translation,

    /// <summary>输出生成阶段。</summary>
    Output
}
namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 爬取结果，包含爬取统计信息。
/// </summary>
public sealed class CrawlResult
{
    /// <summary>仓库标识（owner/repo 格式）</summary>
    public string RepoKey { get; set; } = string.Empty;

    /// <summary>总页面数</summary>
    public int TotalPages { get; set; }

    /// <summary>成功页面数</summary>
    public int SuccessCount { get; set; }

    /// <summary>失败页面数</summary>
    public int FailCount { get; set; }

    /// <summary>总耗时</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>输出路径</summary>
    public string OutputPath { get; set; } = string.Empty;
}

namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 爬取元数据记录，存储于 SQLite crawl_metadata 表。
/// </summary>
public sealed record CrawlMetadata
{
    /// <summary>仓库标识（owner/repo 格式）</summary>
    public string RepoKey { get; init; } = string.Empty;

    /// <summary>爬取开始时间</summary>
    public DateTime StartedAt { get; init; }

    /// <summary>爬取完成时间，未完成时为 null</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>爬取状态</summary>
    public string Status { get; init; } = "Running";

    /// <summary>总页面数</summary>
    public int TotalPages { get; init; }

    /// <summary>成功页面数</summary>
    public int SuccessPages { get; init; }

    /// <summary>失败页面数</summary>
    public int FailedPages { get; init; }
}

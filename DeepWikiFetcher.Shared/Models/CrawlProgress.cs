using DeepWikiFetcher.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 爬取进度报告 DTO，供 CLI 与 MAUI 入口共用。
/// </summary>
public sealed class CrawlProgress
{
    /// <summary>当前处理阶段。</summary>
    public CrawlPhase Phase { get; set; }

    /// <summary>总页面数。</summary>
    public int TotalPages { get; set; }

    /// <summary>已完成页面数。</summary>
    public int CompletedPages { get; set; }

    /// <summary>当前页面标题。</summary>
    public string? CurrentPageTitle { get; set; }

    /// <summary>日志消息。</summary>
    public string? LogMessage { get; set; }

    /// <summary>日志级别。</summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
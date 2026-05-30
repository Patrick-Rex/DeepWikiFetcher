using DeepWikiFetcher.Shared.Enums;

namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 爬取配置，由 CLI 参数和 appsettings.json 合并构建。
/// </summary>
public sealed class CrawlOptions
{
    /// <summary>GitHub 仓库 URL（如 https://github.com/owner/repo）</summary>
    public string GitHubUrl { get; set; } = string.Empty;

    /// <summary>输出根目录</summary>
    public string OutputRoot { get; set; } = string.Empty;

    /// <summary>输出格式</summary>
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Markdown;

    /// <summary>是否启用翻译（本阶段始终为 false）</summary>
    public bool TranslationEnabled { get; set; }
}

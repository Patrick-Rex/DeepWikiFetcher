namespace DeepWikiFetcher.Shared.Models;

/// <summary>
/// 文档目录树节点，代表 DeepWiki 文档的完整目录结构。
/// </summary>
public sealed class DocumentNode
{
    /// <summary>原始标题（英文）</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>翻译后的标题，初始为 null</summary>
    public string? TranslatedTitle { get; set; }

    /// <summary>页面绝对 URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>层级深度（根节点为 0）</summary>
    public int Depth { get; set; }

    /// <summary>层级编号（如 "1"、"2.3"、"1.1.2"）</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>子节点集合</summary>
    public List<DocumentNode> Children { get; set; } = [];

    /// <summary>页面清洗后的 HTML 内容（爬取阶段填充）</summary>
    public string? Content { get; set; }
}

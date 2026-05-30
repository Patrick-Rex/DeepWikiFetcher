using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 使用 AngleSharp 解析 DeepWiki 页面侧边栏，递归构建 DocumentNode 目录树。
/// </summary>
public sealed class SidebarParser : ISidebarParser
{
    private readonly IPageFetcher _pageFetcher;
    private readonly IHtmlParser _htmlParser;
    private readonly ILogger<SidebarParser> _logger;

    public SidebarParser(IPageFetcher pageFetcher, ILogger<SidebarParser> logger)
    {
        _pageFetcher = pageFetcher;
        _htmlParser = new HtmlParser(new HtmlParserOptions { IsStrictMode = false });
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DocumentNode> ParseAsync(string deepWikiHomeUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Sidebar parse start: {Url}", deepWikiHomeUrl);

        var html = await _pageFetcher.FetchAsync(deepWikiHomeUrl, ct);
        var document = await _htmlParser.ParseDocumentAsync(html, ct);

        // 查找侧边栏导航元素
        var sidebarNav = document.QuerySelector("nav.sidebar, nav[role='navigation'], aside nav, nav.menu");
        if (sidebarNav is null)
        {
            _logger.LogWarning("No sidebar nav found for {Url}, returning empty tree", deepWikiHomeUrl);
            return new DocumentNode
            {
                Title = "Root",
                Url = deepWikiHomeUrl,
                Depth = 0,
                Number = "0",
                Children = []
            };
        }

        // 查找顶层 ul
        var topUl = sidebarNav.QuerySelector("ul");
        if (topUl is null)
        {
            _logger.LogWarning("No ul element in sidebar for {Url}", deepWikiHomeUrl);
            return new DocumentNode
            {
                Title = "Root",
                Url = deepWikiHomeUrl,
                Depth = 0,
                Number = "0",
                Children = []
            };
        }

        var root = new DocumentNode
        {
            Title = "Root",
            Url = deepWikiHomeUrl,
            Depth = 0,
            Number = "0",
            Children = []
        };

        // 递归解析 li 层级
        ParseList(topUl, root, deepWikiHomeUrl);

        _logger.LogInformation("Sidebar parse complete: {NodeCount} nodes", CountNodes(root));
        return root;
    }

    private void ParseList(IElement ul, DocumentNode parent, string baseUrl, string parentNumber = "")
    {
        var index = 0;
        foreach (var li in ul.QuerySelectorAll(":scope > li"))
        {
            index++;
            var number = string.IsNullOrEmpty(parentNumber)
                ? index.ToString()
                : $"{parentNumber}.{index}";

            // 提取链接和标题
            var link = li.QuerySelector("a");
            var title = link?.TextContent.Trim() ?? li.TextContent.Trim();
            var href = link?.GetAttribute("href") ?? "";

            // 构建绝对 URL
            var absoluteUrl = ResolveUrl(href, baseUrl);

            var node = new DocumentNode
            {
                Title = title,
                Url = absoluteUrl,
                Depth = parent.Depth + 1,
                Number = number,
                Children = []
            };

            // 递归处理子列表
            var childUl = li.QuerySelector(":scope > ul, :scope > ol");
            if (childUl is not null)
            {
                ParseList(childUl, node, baseUrl, number);
            }

            parent.Children.Add(node);
        }
    }

    private static string ResolveUrl(string href, string baseUrl)
    {
        if (string.IsNullOrEmpty(href))
            return baseUrl;

        if (Uri.TryCreate(href, UriKind.Absolute, out _))
            return href;

        if (Uri.TryCreate(new Uri(baseUrl), href, out var resolved))
            return resolved.ToString();

        return baseUrl;
    }

    private static int CountNodes(DocumentNode node)
    {
        int count = 1;
        foreach (var child in node.Children)
        {
            count += CountNodes(child);
        }
        return count;
    }
}

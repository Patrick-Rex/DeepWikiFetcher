using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 使用 AngleSharp 清洗 DeepWiki HTML：移除 nav/footer，保留 article/content，提取 img 引用。
/// </summary>
public sealed class HtmlCleaner : IHtmlCleaner
{
    private readonly IHtmlParser _htmlParser;
    private readonly ILogger<HtmlCleaner> _logger;

    public HtmlCleaner(ILogger<HtmlCleaner> logger)
    {
        _htmlParser = new HtmlParser(new HtmlParserOptions { IsStrictMode = false });
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CleanResult> CleanAsync(string rawHtml, string baseUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("HTML clean start: baseUrl={BaseUrl}", baseUrl);

        var document = await _htmlParser.ParseDocumentAsync(rawHtml, ct);
        var imageUrls = new List<string>();

        // 移除导航元素
        foreach (var nav in document.QuerySelectorAll("nav, header, footer"))
        {
            nav.Remove();
        }

        // 提取文章主体
        IElement? content = document.QuerySelector("article, .content, .markdown-body, main");
        if (content is null)
        {
            _logger.LogWarning("No article/content element found, falling back to body");
            content = document.Body ?? document.DocumentElement;
        }

        // 移除脚本和样式
        foreach (var el in content.QuerySelectorAll("script, style, noscript"))
        {
            el.Remove();
        }

        // 修复相对链接为绝对链接
        FixRelativeLinks(content, baseUrl);

        // 提取图片引用
        foreach (var img in content.QuerySelectorAll("img"))
        {
            var src = img.GetAttribute("src");
            if (!string.IsNullOrEmpty(src))
            {
                var absoluteSrc = ResolveUrl(src, baseUrl);
                imageUrls.Add(absoluteSrc);
            }
        }

        _logger.LogInformation("HTML clean complete: {ImageCount} images extracted", imageUrls.Count);

        return new CleanResult
        {
            CleanHtml = content.OuterHtml,
            ImageUrls = imageUrls
        };
    }

    private static void FixRelativeLinks(IElement element, string baseUrl)
    {
        // 修复 a 标签的 href
        foreach (var a in element.QuerySelectorAll("a[href]"))
        {
            var href = a.GetAttribute("href");
            if (href is not null && !Uri.TryCreate(href, UriKind.Absolute, out _))
            {
                var resolved = ResolveUrl(href, baseUrl);
                a.SetAttribute("href", resolved);
            }
        }

        // 修复 img 标签的 src
        foreach (var img in element.QuerySelectorAll("img[src]"))
        {
            var src = img.GetAttribute("src");
            if (src is not null && !Uri.TryCreate(src, UriKind.Absolute, out _))
            {
                var resolved = ResolveUrl(src, baseUrl);
                img.SetAttribute("src", resolved);
            }
        }
    }

    private static string ResolveUrl(string relativeUrl, string baseUrl)
    {
        if (Uri.TryCreate(new Uri(baseUrl), relativeUrl, out var resolved))
        {
            return resolved.ToString();
        }
        return relativeUrl;
    }
}

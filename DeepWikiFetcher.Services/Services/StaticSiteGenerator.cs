using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Enums;
using DeepWikiFetcher.Shared.Models;
using Markdig;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 静态站点输出生成器。
/// </summary>
public sealed class StaticSiteGenerator : IOutputGenerator
{
    private const string EnglishLanguage = "en";
    private const string ChineseLanguage = "zh-cn";
    private const string PagesDirectoryName = "pages";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private readonly ILogger<StaticSiteGenerator> _logger;

    public StaticSiteGenerator(ILogger<StaticSiteGenerator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.StaticSite;

    /// <inheritdoc />
    public async Task GenerateAsync(DocumentNode root, string outputDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        await WriteSharedAssetsAsync(outputDir, ct);

        bool hasChinese = HasTranslatedContent(root);
        var languages = hasChinese ? [EnglishLanguage, ChineseLanguage] : new List<string> { EnglishLanguage };

        foreach (string language in languages)
        {
            await GenerateLanguageAsync(root, outputDir, language, languages, ct);
        }

        await WriteRootIndexAsync(outputDir, hasChinese, ct);
        await File.WriteAllTextAsync(Path.Combine(outputDir, ".nojekyll"), string.Empty, ct);
        _logger.LogInformation("Static site generation complete: {OutputDir}", outputDir);
    }

    private async Task GenerateLanguageAsync(
        DocumentNode root,
        string outputDir,
        string language,
        List<string> languages,
        CancellationToken ct)
    {
        var languageDirectory = Path.Combine(outputDir, language);
        var pagesDirectory = Path.Combine(languageDirectory, PagesDirectoryName);
        Directory.CreateDirectory(pagesDirectory);

        var pages = new List<DocumentNode>();
        CollectPages(root, pages);

        var sidebar = root.Children.Select(child => ToSidebarEntry(child, language)).ToList();
        await WriteJsonAsync(Path.Combine(languageDirectory, "sidebar.json"), sidebar, ct);

        var config = new StaticSiteConfig
        {
            SiteTitle = GetTitle(root, language),
            DefaultLanguage = language,
            AvailableLanguages = languages,
            RepoKey = string.Empty
        };
        string configJson = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(languageDirectory, "config.js"), $"window.DeepWikiFetcherConfig = {configJson};", ct);

        foreach (var page in pages)
        {
            ct.ThrowIfCancellationRequested();
            string fileName = GetPageFileName(page);
            string targetPath = Path.Combine(pagesDirectory, fileName);
            string title = GetTitle(page, language);
            string markdown = NormalizeAssetPaths(GetContent(page, language));
            string html = Markdown.ToHtml(markdown, _markdownPipeline);
            string document = BuildPageHtml(language, title, config.SiteTitle, html, "../../assets");
            await File.WriteAllTextAsync(targetPath, document, Encoding.UTF8, ct);
        }

        string firstPage = pages.Count > 0 ? $"pages/{GetPageFileName(pages[0])}" : string.Empty;
        await WriteLanguageIndexAsync(languageDirectory, language, firstPage, ct);
    }

    private static async Task WriteSharedAssetsAsync(string outputDir, CancellationToken ct)
    {
        var cssDirectory = Path.Combine(outputDir, "assets", "css");
        var jsDirectory = Path.Combine(outputDir, "assets", "js");
        Directory.CreateDirectory(cssDirectory);
        Directory.CreateDirectory(jsDirectory);

        const string css = """
            :root { color-scheme: light; font-family: Georgia, 'Times New Roman', serif; --text: #1d2528; --muted: #607077; --line: #d8e0dd; --accent: #2f7d68; --paper: #fbfbf7; --nav: #eef3ef; }
            * { box-sizing: border-box; }
            body { margin: 0; background: var(--paper); color: var(--text); display: grid; grid-template-columns: minmax(220px, 300px) 1fr; min-height: 100vh; }
            .sidebar { background: var(--nav); border-right: 1px solid var(--line); padding: 24px; position: sticky; top: 0; height: 100vh; overflow: auto; }
            .content { max-width: 920px; padding: 48px 40px 72px; line-height: 1.7; }
            a { color: var(--accent); }
            pre, code { font-family: 'Cascadia Code', Consolas, monospace; }
            pre { overflow: auto; padding: 16px; background: #17211f; color: #f2f7f4; border-radius: 6px; }
            img { max-width: 100%; height: auto; }
            @media (max-width: 760px) { body { display: block; } .sidebar { height: auto; position: static; border-right: 0; border-bottom: 1px solid var(--line); } .content { padding: 28px 20px 48px; } }
            """;

        const string js = """
            document.querySelectorAll('a[href]').forEach(link => {
              if (link.href === location.href) link.setAttribute('aria-current', 'page');
            });
            """;

        await File.WriteAllTextAsync(Path.Combine(cssDirectory, "style.css"), css, Encoding.UTF8, ct);
        await File.WriteAllTextAsync(Path.Combine(jsDirectory, "sidebar.js"), js, Encoding.UTF8, ct);
    }

    private static string BuildPageHtml(string language, string title, string siteTitle, string contentHtml, string assetsRoot)
    {
        string safeTitle = HtmlEncoder.Default.Encode(title);
        string safeSiteTitle = HtmlEncoder.Default.Encode(siteTitle);
        return $$"""
            <!DOCTYPE html>
            <html lang="{{language}}">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{{safeTitle}} - {{safeSiteTitle}}</title>
                <link rel="stylesheet" href="{{assetsRoot}}/css/style.css">
            </head>
            <body>
                <nav class="sidebar">
                    <strong>{{safeSiteTitle}}</strong>
                    <p><a href="../index.html">Index</a></p>
                </nav>
                <main class="content">
                    {{contentHtml}}
                </main>
                <script src="{{assetsRoot}}/js/sidebar.js"></script>
            </body>
            </html>
            """;
    }

    private static async Task WriteRootIndexAsync(string outputDir, bool hasChinese, CancellationToken ct)
    {
        string body = hasChinese
            ? "<p>Redirecting... <a href=\"en/index.html\">English</a> | <a href=\"zh-cn/index.html\">中文</a></p>"
            : "<p>Redirecting... <a href=\"en/index.html\">English</a></p>";
        string script = hasChinese
            ? "const lang = navigator.language.toLowerCase(); location.href = lang.startsWith('zh') ? 'zh-cn/index.html' : 'en/index.html';"
            : "location.href = 'en/index.html';";
        string html = $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="UTF-8"><script>{script}</script></head>
            <body>{body}</body>
            </html>
            """;
        await File.WriteAllTextAsync(Path.Combine(outputDir, "index.html"), html, Encoding.UTF8, ct);
    }

    private static async Task WriteLanguageIndexAsync(string languageDirectory, string language, string firstPage, CancellationToken ct)
    {
        string html = $"""
            <!DOCTYPE html>
            <html lang="{language}">
            <head><meta charset="UTF-8"><script>location.href = '{firstPage}';</script></head>
            <body><p><a href="{firstPage}">Open documentation</a></p></body>
            </html>
            """;
        await File.WriteAllTextAsync(Path.Combine(languageDirectory, "index.html"), html, Encoding.UTF8, ct);
    }

    private static Task WriteJsonAsync<T>(string filePath, T value, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return File.WriteAllTextAsync(filePath, json, Encoding.UTF8, ct);
    }

    private static SidebarEntry ToSidebarEntry(DocumentNode node, string language)
    {
        return new SidebarEntry
        {
            Title = GetTitle(node, language),
            Path = $"/{language}/{PagesDirectoryName}/{GetPageFileName(node)}",
            Children = node.Children.Count == 0
                ? null
                : node.Children.Select(child => ToSidebarEntry(child, language)).ToList()
        };
    }

    private static void CollectPages(DocumentNode node, List<DocumentNode> pages)
    {
        if (node.Depth > 0 && !string.IsNullOrWhiteSpace(node.Url))
        {
            pages.Add(node);
        }

        foreach (var child in node.Children)
        {
            CollectPages(child, pages);
        }
    }

    private static bool HasTranslatedContent(DocumentNode root)
    {
        var pages = new List<DocumentNode>();
        CollectPages(root, pages);
        return pages.Any(page => !string.IsNullOrWhiteSpace(page.TranslatedContent));
    }

    private static string GetTitle(DocumentNode node, string language)
    {
        return language == ChineseLanguage && !string.IsNullOrWhiteSpace(node.TranslatedTitle)
            ? node.TranslatedTitle
            : node.Title;
    }

    private static string GetContent(DocumentNode node, string language)
    {
        if (language == ChineseLanguage && !string.IsNullOrWhiteSpace(node.TranslatedContent))
        {
            return node.TranslatedContent;
        }

        return node.Content ?? $"*Content will be fetched from: {node.Url}*";
    }

    private static string GetPageFileName(DocumentNode node)
    {
        string number = string.IsNullOrWhiteSpace(node.Number) ? "0" : node.Number;
        return $"{number}-{MarkdownWriter.GenerateSlug(node.Title)}.html";
    }

    private static string NormalizeAssetPaths(string markdown)
    {
        return markdown
            .Replace("assets/images/", "../../assets/images/", StringComparison.OrdinalIgnoreCase)
            .Replace("../../../../assets/images/", "../../assets/images/", StringComparison.OrdinalIgnoreCase);
    }
}
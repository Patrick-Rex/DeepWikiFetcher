using DeepWikiFetcher.Services.Services;
using DeepWikiFetcher.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DeepWikiFetcher.Tests.Services;

/// <summary>
/// StaticSiteGenerator 单元测试。
/// </summary>
public sealed class StaticSiteGeneratorTests
{
    private const long MaxAssetBytes = 50 * 1024;

    [Fact]
    public async Task GenerateAsync_ShouldCreateEnglishOnlyStaticSiteWhenTranslationIsMissing()
    {
        var outputDir = CreateTempDirectory();
        var generator = new StaticSiteGenerator(NullLogger<StaticSiteGenerator>.Instance);
        var root = CreateRoot(includeChinese: false);

        await generator.GenerateAsync(root, outputDir);

        File.Exists(Path.Combine(outputDir, "index.html")).Should().BeTrue();
        Directory.Exists(Path.Combine(outputDir, "en", "pages")).Should().BeTrue();
        Directory.Exists(Path.Combine(outputDir, "zh-cn")).Should().BeFalse();
        File.Exists(Path.Combine(outputDir, "en", "sidebar.json")).Should().BeTrue();
        File.Exists(Path.Combine(outputDir, "en", "config.js")).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_ShouldCreateBilingualStaticSiteWhenTranslationExists()
    {
        var outputDir = CreateTempDirectory();
        var generator = new StaticSiteGenerator(NullLogger<StaticSiteGenerator>.Instance);
        var root = CreateRoot(includeChinese: true);

        await generator.GenerateAsync(root, outputDir);

        File.Exists(Path.Combine(outputDir, "en", "pages", "1-overview.html")).Should().BeTrue();
        File.Exists(Path.Combine(outputDir, "zh-cn", "pages", "1-overview.html")).Should().BeTrue();
        string chinesePage = await File.ReadAllTextAsync(Path.Combine(outputDir, "zh-cn", "pages", "1-overview.html"));
        chinesePage.Should().Contain("中文内容");
    }

    [Fact]
    public async Task GenerateAsync_ShouldKeepCssAndJavaScriptUnderSizeLimit()
    {
        var outputDir = CreateTempDirectory();
        var generator = new StaticSiteGenerator(NullLogger<StaticSiteGenerator>.Instance);

        await generator.GenerateAsync(CreateRoot(includeChinese: false), outputDir);

        var cssInfo = new FileInfo(Path.Combine(outputDir, "assets", "css", "style.css"));
        var jsInfo = new FileInfo(Path.Combine(outputDir, "assets", "js", "sidebar.js"));
        (cssInfo.Length + jsInfo.Length).Should().BeLessThan(MaxAssetBytes);
    }

    private static DocumentNode CreateRoot(bool includeChinese)
    {
        return new DocumentNode
        {
            Title = "DeepWikiFetcher",
            TranslatedTitle = includeChinese ? "DeepWikiFetcher" : null,
            Children =
            [
                new DocumentNode
                {
                    Title = "Overview",
                    TranslatedTitle = includeChinese ? "概览" : null,
                    Url = "https://deepwiki.com/owner/repo/overview",
                    Depth = 1,
                    Number = "1",
                    Content = "# Overview\n\nEnglish content with ![image](assets/images/demo.png).",
                    TranslatedContent = includeChinese ? "# 概览\n\n中文内容。" : null
                }
            ]
        };
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "DeepWikiFetcher.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
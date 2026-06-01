using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Services.Services;
using DeepWikiFetcher.Shared.Models;
using DeepWikiFetcher.Shared.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DeepWikiFetcher.Tests.Services;

/// <summary>
/// TranslationService 单元测试。
/// </summary>
public sealed class TranslationServiceTests
{
    [Fact]
    public async Task TranslateBatchAsync_ShouldRestoreProtectedMarkdownTokens()
    {
        var apiClient = Substitute.For<ITranslationApiClient>();
        var cacheManager = Substitute.For<ICacheManager>();
        var service = CreateService(apiClient, cacheManager);
        var root = CreateRoot("Hello `dotnet build` https://example.com");

        apiClient.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => ((string)call[0]).Replace("Hello", "你好", StringComparison.Ordinal));

        var result = await service.TranslateBatchAsync(root);

        result.Children[0].TranslatedContent.Should().Contain("你好");
        result.Children[0].TranslatedContent.Should().Contain("`dotnet build`");
        result.Children[0].TranslatedContent.Should().Contain("https://example.com");
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldUseTranslationCacheWhenAvailable()
    {
        var apiClient = Substitute.For<ITranslationApiClient>();
        var cacheManager = Substitute.For<ICacheManager>();
        var service = CreateService(apiClient, cacheManager);
        var root = CreateRoot("Cached source");

        cacheManager.GetTranslationAsync("Cached source", "test-model")
            .Returns(new TranslationCacheEntry
            {
                SourceHash = "hash",
                PageUrl = "https://deepwiki.com/owner/repo/page",
                SourceText = "Cached source",
                TranslatedText = "缓存内容",
                Model = "test-model",
                CachedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            });
        apiClient.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("缓存标题");

        var result = await service.TranslateBatchAsync(root);

        result.Children[0].TranslatedContent.Should().Be("缓存内容");
        await cacheManager.DidNotReceive().SetTranslationAsync(Arg.Any<TranslationCacheEntry>());
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldDegradeToSourceTextWhenApiFails()
    {
        var apiClient = Substitute.For<ITranslationApiClient>();
        var cacheManager = Substitute.For<ICacheManager>();
        var service = CreateService(apiClient, cacheManager);
        var root = CreateRoot("Original content");

        apiClient.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new HttpRequestException("boom"));

        var result = await service.TranslateBatchAsync(root);

        result.Children[0].TranslatedTitle.Should().Be("Overview");
        result.Children[0].TranslatedContent.Should().Be("Original content");
    }

    [Fact]
    public async Task TranslateBatchAsync_ShouldProcessAllPagesAcrossBatches()
    {
        var apiClient = Substitute.For<ITranslationApiClient>();
        var cacheManager = Substitute.For<ICacheManager>();
        var service = CreateService(apiClient, cacheManager, batchSize: 2);
        var root = new DocumentNode
        {
            Title = "Root",
            Children =
            [
                CreatePage("1", "One"),
                CreatePage("2", "Two"),
                CreatePage("3", "Three")
            ]
        };

        apiClient.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => $"zh:{(string)call[0]}");

        var result = await service.TranslateBatchAsync(root);

        result.Children.Should().OnlyContain(node => node.TranslatedContent != null);
        await cacheManager.Received(3).SetTranslationAsync(Arg.Any<TranslationCacheEntry>());
    }

    private static TranslationService CreateService(
        ITranslationApiClient apiClient,
        ICacheManager cacheManager,
        int batchSize = 10)
    {
        var options = Options.Create(new TranslationOptions
        {
            Enabled = true,
            Model = "test-model",
            BatchSize = batchSize,
            MaxConcurrency = 1,
            CacheExpirationDays = 30,
            RequestDelayMs = 0
        });

        return new TranslationService(apiClient, cacheManager, options, NullLogger<TranslationService>.Instance);
    }

    private static DocumentNode CreateRoot(string content)
    {
        return new DocumentNode
        {
            Title = "Root",
            Children = [CreatePage("1", content)]
        };
    }

    private static DocumentNode CreatePage(string number, string content)
    {
        return new DocumentNode
        {
            Title = "Overview",
            Url = $"https://deepwiki.com/owner/repo/{number}",
            Depth = 1,
            Number = number,
            Content = content
        };
    }
}
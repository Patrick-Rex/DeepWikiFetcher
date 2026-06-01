using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Enums;
using DeepWikiFetcher.Shared.Models;
using DeepWikiFetcher.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 翻译服务：批量翻译文档树并维护翻译缓存。
/// </summary>
public sealed partial class TranslationService : ITranslationService
{
    private readonly ITranslationApiClient _translationApiClient;
    private readonly ICacheManager _cacheManager;
    private readonly TranslationOptions _options;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(
        ITranslationApiClient translationApiClient,
        ICacheManager cacheManager,
        IOptions<TranslationOptions> options,
        ILogger<TranslationService> logger)
    {
        _translationApiClient = translationApiClient;
        _cacheManager = cacheManager;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DocumentNode> TranslateBatchAsync(
        DocumentNode root,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return root;
        }

        var pages = new List<DocumentNode>();
        CollectPages(root, pages);
        int completed = 0;

        foreach (var batch in pages.Chunk(Math.Max(1, _options.BatchSize)))
        {
            using var semaphore = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrency));
            var tasks = batch.Select(node => TranslateNodeAsync(node, semaphore, cancellationToken)).ToList();
            foreach (var task in tasks)
            {
                var translatedNode = await task;
                completed++;
                progress?.Report(new CrawlProgress
                {
                    Phase = CrawlPhase.Translation,
                    TotalPages = pages.Count,
                    CompletedPages = completed,
                    CurrentPageTitle = translatedNode.Title,
                    LogMessage = $"Translated {translatedNode.Title}"
                });
            }
        }

        return root;
    }

    private async Task<DocumentNode> TranslateNodeAsync(
        DocumentNode node,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(node.Content))
            {
                _logger.LogWarning("Skip translation because content is empty: {Url}", node.Url);
                return node;
            }

            var cached = await _cacheManager.GetTranslationAsync(node.Content, _options.Model);
            if (cached is not null)
            {
                node.TranslatedContent = cached.TranslatedText;
                node.TranslatedTitle = await TranslateProtectedAsync(node.Title, cancellationToken);
                return node;
            }

            node.TranslatedTitle = await TranslateProtectedAsync(node.Title, cancellationToken);
            string translatedContent = await TranslateProtectedAsync(node.Content, cancellationToken);
            node.TranslatedContent = translatedContent;

            var now = DateTimeOffset.UtcNow;
            await _cacheManager.SetTranslationAsync(new TranslationCacheEntry
            {
                SourceHash = ComputeHash(node.Content),
                PageUrl = node.Url,
                SourceText = node.Content,
                TranslatedText = translatedContent,
                Model = _options.Model,
                CachedAt = now,
                ExpiresAt = now.AddDays(Math.Max(1, _options.CacheExpirationDays))
            });

            return node;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Translation failed and degraded to source text: {Url}", node.Url);
            node.TranslatedTitle = node.Title;
            node.TranslatedContent = node.Content;
            return node;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<string> TranslateProtectedAsync(string sourceText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return sourceText;
        }

        string protectedText = ProtectMarkdown(sourceText, out var placeholders);
        if (_options.RequestDelayMs > 0)
        {
            await Task.Delay(_options.RequestDelayMs, cancellationToken);
        }

        string translated = await _translationApiClient.TranslateAsync(protectedText, _options.Model, cancellationToken);
        foreach (var placeholder in placeholders)
        {
            translated = translated.Replace(placeholder.Key, placeholder.Value, StringComparison.Ordinal);
        }

        return translated;
    }

    private static string ProtectMarkdown(string sourceText, out Dictionary<string, string> placeholders)
    {
        placeholders = [];
        string protectedText = ReplaceMatches(CodeBlockRegex(), sourceText, "CODE_BLOCK", placeholders);
        protectedText = ReplaceMatches(InlineCodeRegex(), protectedText, "INLINE_CODE", placeholders);
        protectedText = ReplaceMatches(UrlRegex(), protectedText, "URL", placeholders);
        return protectedText;
    }

    private static string ReplaceMatches(
        Regex regex,
        string sourceText,
        string prefix,
        Dictionary<string, string> placeholders)
    {
        return regex.Replace(sourceText, match =>
        {
            string placeholder = $"__{prefix}_{placeholders.Count}__";
            placeholders[placeholder] = match.Value;
            return placeholder;
        });
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

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    [GeneratedRegex("```[\\s\\S]*?```", RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex("`[^`\\r\\n]+`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex("https?://[^\\s)]+")]
    private static partial Regex UrlRegex();
}
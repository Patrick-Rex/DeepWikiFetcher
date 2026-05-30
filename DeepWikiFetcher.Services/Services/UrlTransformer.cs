using DeepWikiFetcher.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 将 GitHub URL 映射为 DeepWiki URL。
/// 规则：github.com/owner/repo[/...] → deepwiki.com/owner/repo[/...]
/// </summary>
public sealed class UrlTransformer : IUrlTransformer
{
    private readonly ILogger<UrlTransformer> _logger;

    public UrlTransformer(ILogger<UrlTransformer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Transform(string githubUrl)
    {
        _logger.LogInformation("URL transform: input = {GitHubUrl}", githubUrl);

        if (string.IsNullOrWhiteSpace(githubUrl))
        {
            throw new ArgumentException("GitHub URL must not be empty.", nameof(githubUrl));
        }

        if (!Uri.TryCreate(githubUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL format: {githubUrl}", nameof(githubUrl));
        }

        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"URL must be a github.com URL, got: {uri.Host}", nameof(githubUrl));
        }

        // Extract owner/repo/path from GitHub URL
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2)
        {
            throw new ArgumentException(
                $"Invalid GitHub URL: expected github.com/owner/repo, got: {githubUrl}",
                nameof(githubUrl));
        }

        var owner = segments[0];
        var repo = segments[1];

        // Build path suffix: skip /tree/{branch}/ if present
        var pathSuffix = string.Empty;
        if (segments.Length > 2)
        {
            var remaining = new List<string>();
            bool skipNext = false;
            foreach (var seg in segments.Skip(2))
            {
                if (seg is "tree" or "blob")
                {
                    skipNext = true;
                    continue;
                }
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }
                remaining.Add(seg);
            }
            if (remaining.Count > 0)
            {
                pathSuffix = string.Join('/', remaining);
            }
        }

        var deepWikiUrl = pathSuffix.Length > 0
            ? $"https://deepwiki.com/{owner}/{repo}/{pathSuffix}"
            : $"https://deepwiki.com/{owner}/{repo}";

        _logger.LogInformation("URL transform: output = {DeepWikiUrl}", deepWikiUrl);
        return deepWikiUrl;
    }
}

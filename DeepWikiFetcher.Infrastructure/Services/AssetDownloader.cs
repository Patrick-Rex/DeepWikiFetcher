using System.Security.Cryptography;
using System.Text;
using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Infrastructure.Services;

/// <summary>
/// 图片资源下载器。
/// </summary>
public sealed class AssetDownloader : IAssetDownloader
{
    private const int MaxRetryAttempts = 2;
    private const string AssetsDirectoryName = "assets";
    private const string ImagesDirectoryName = "images";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AssetDownloader> _logger;

    public AssetDownloader(IHttpClientFactory httpClientFactory, ILogger<AssetDownloader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<AssetInfo>> DownloadAsync(
        List<string> imageUrls,
        string outputRoot,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AssetInfo>();
        if (imageUrls.Count == 0)
        {
            return results;
        }

        var imagesDirectory = Path.Combine(outputRoot, AssetsDirectoryName, ImagesDirectoryName);
        Directory.CreateDirectory(imagesDirectory);

        foreach (string imageUrl in imageUrls.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!IsDeepWikiImage(imageUrl))
            {
                results.Add(new AssetInfo { OriginalUrl = imageUrl, Downloaded = false });
                continue;
            }

            string fileName = BuildFileName(imageUrl);
            var targetPath = Path.Combine(imagesDirectory, fileName);
            bool downloaded = await DownloadOneAsync(imageUrl, targetPath, cancellationToken);
            results.Add(new AssetInfo
            {
                OriginalUrl = imageUrl,
                LocalFileName = fileName,
                Downloaded = downloaded
            });
        }

        return results;
    }

    private async Task<bool> DownloadOneAsync(string imageUrl, string targetPath, CancellationToken cancellationToken)
    {
        if (File.Exists(targetPath))
        {
            return true;
        }

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync(imageUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Image download failed: {Url}, status={Status}", imageUrl, response.StatusCode);
                    continue;
                }

                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var destination = File.Create(targetPath);
                await source.CopyToAsync(destination, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                _logger.LogWarning(ex, "Image download retry: {Url}, attempt={Attempt}", imageUrl, attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image download failed: {Url}", imageUrl);
                return false;
            }
        }

        return false;
    }

    private static bool IsDeepWikiImage(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host.Equals("deepwiki.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".deepwiki.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildFileName(string imageUrl)
    {
        var uri = new Uri(imageUrl);
        string extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(imageUrl));
        return $"{Convert.ToHexStringLower(bytes)}{extension}";
    }
}
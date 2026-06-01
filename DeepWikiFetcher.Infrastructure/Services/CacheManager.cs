using System.Security.Cryptography;
using System.Text;
using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Shared.Models;
using DeepWikiFetcher.Shared.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeepWikiFetcher.Infrastructure.Services;

/// <summary>
/// SQLite 缓存管理器：管理 page_cache 和 crawl_metadata 表。
/// 缓存键 = URL 的 SHA256 哈希，默认 24 小时过期。
/// </summary>
public sealed class CacheManager : ICacheManager, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CrawlerOptions _options;
    private readonly ILogger<CacheManager> _logger;
    private bool _initialized;

    public CacheManager(
        IOptions<CrawlerOptions> options,
        ILogger<CacheManager> logger)
    {
        _options = options.Value;
        _logger = logger;

        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "cache.db");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        if (_initialized) return;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS page_cache (
                url_hash TEXT PRIMARY KEY,
                url TEXT NOT NULL,
                content TEXT NOT NULL,
                cached_at TEXT NOT NULL,
                expires_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS crawl_metadata (
                repo_key TEXT PRIMARY KEY,
                started_at TEXT NOT NULL,
                completed_at TEXT,
                status TEXT NOT NULL DEFAULT 'Running',
                total_pages INTEGER NOT NULL DEFAULT 0,
                success_pages INTEGER NOT NULL DEFAULT 0,
                failed_pages INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_page_cache_expires ON page_cache(expires_at);

            CREATE TABLE IF NOT EXISTS translation_cache (
                source_hash TEXT PRIMARY KEY,
                page_url TEXT NOT NULL,
                source_text TEXT NOT NULL,
                translated_text TEXT NOT NULL,
                model TEXT NOT NULL DEFAULT '',
                cached_at TEXT NOT NULL,
                expires_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_translation_cache_page_url ON translation_cache(page_url);
            CREATE INDEX IF NOT EXISTS idx_translation_cache_model ON translation_cache(model);
            CREATE INDEX IF NOT EXISTS idx_translation_cache_expires ON translation_cache(expires_at);
            """;
        cmd.ExecuteNonQuery();
        _initialized = true;
        _logger.LogInformation("SQLite cache initialized");
    }

    /// <inheritdoc />
    public async Task<string?> GetPageAsync(string url)
    {
        var hash = ComputeHash(url);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT content FROM page_cache
            WHERE url_hash = @hash AND expires_at > @now
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));

        var result = await cmd.ExecuteScalarAsync();
        if (result is string content)
        {
            _logger.LogInformation("Cache HIT: {Url}", url);
            return content;
        }

        _logger.LogInformation("Cache MISS: {Url}", url);
        return null;
    }

    /// <inheritdoc />
    public async Task SetPageAsync(string url, string content)
    {
        var hash = ComputeHash(url);
        var now = DateTime.UtcNow;
        var expires = now.AddHours(_options.CacheExpirationHours);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO page_cache (url_hash, url, content, cached_at, expires_at)
            VALUES (@hash, @url, @content, @cachedAt, @expiresAt)
            """;
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@url", url);
        cmd.Parameters.AddWithValue("@content", content);
        cmd.Parameters.AddWithValue("@cachedAt", now.ToString("O"));
        cmd.Parameters.AddWithValue("@expiresAt", expires.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Cache SET: {Url}, expires={Expires}", url, expires);
    }

    /// <inheritdoc />
    public async Task SaveMetadataAsync(CrawlMetadata metadata)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO crawl_metadata
                (repo_key, started_at, completed_at, status, total_pages, success_pages, failed_pages)
            VALUES
                (@repoKey, @startedAt, @completedAt, @status, @totalPages, @successPages, @failedPages)
            """;
        cmd.Parameters.AddWithValue("@repoKey", metadata.RepoKey);
        cmd.Parameters.AddWithValue("@startedAt", metadata.StartedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@completedAt", (object?)metadata.CompletedAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", metadata.Status);
        cmd.Parameters.AddWithValue("@totalPages", metadata.TotalPages);
        cmd.Parameters.AddWithValue("@successPages", metadata.SuccessPages);
        cmd.Parameters.AddWithValue("@failedPages", metadata.FailedPages);

        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Metadata saved: {RepoKey}, status={Status}", metadata.RepoKey, metadata.Status);
    }

    /// <inheritdoc />
    public async Task<CrawlMetadata?> GetMetadataAsync(string repoKey)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM crawl_metadata WHERE repo_key = @repoKey LIMIT 1";
        cmd.Parameters.AddWithValue("@repoKey", repoKey);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CrawlMetadata
            {
                RepoKey = reader.GetString(0),
                StartedAt = DateTime.Parse(reader.GetString(1)),
                CompletedAt = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2)),
                Status = reader.GetString(3),
                TotalPages = reader.GetInt32(4),
                SuccessPages = reader.GetInt32(5),
                FailedPages = reader.GetInt32(6)
            };
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TranslationCacheEntry?> GetTranslationAsync(string sourceText, string model)
    {
        var hash = ComputeHash(sourceText);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT source_hash, page_url, source_text, translated_text, model, cached_at, expires_at
            FROM translation_cache
            WHERE source_hash = @hash AND model = @model AND expires_at > @now
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@model", model);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            _logger.LogInformation("Translation cache MISS: {Hash}", hash);
            return null;
        }

        _logger.LogInformation("Translation cache HIT: {Hash}", hash);
        return new TranslationCacheEntry
        {
            SourceHash = reader.GetString(0),
            PageUrl = reader.GetString(1),
            SourceText = reader.GetString(2),
            TranslatedText = reader.GetString(3),
            Model = reader.GetString(4),
            CachedAt = DateTimeOffset.Parse(reader.GetString(5)),
            ExpiresAt = DateTimeOffset.Parse(reader.GetString(6))
        };
    }

    /// <inheritdoc />
    public async Task SetTranslationAsync(TranslationCacheEntry entry)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO translation_cache
                (source_hash, page_url, source_text, translated_text, model, cached_at, expires_at)
            VALUES
                (@sourceHash, @pageUrl, @sourceText, @translatedText, @model, @cachedAt, @expiresAt)
            """;
        cmd.Parameters.AddWithValue("@sourceHash", entry.SourceHash);
        cmd.Parameters.AddWithValue("@pageUrl", entry.PageUrl);
        cmd.Parameters.AddWithValue("@sourceText", entry.SourceText);
        cmd.Parameters.AddWithValue("@translatedText", entry.TranslatedText);
        cmd.Parameters.AddWithValue("@model", entry.Model);
        cmd.Parameters.AddWithValue("@cachedAt", entry.CachedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@expiresAt", entry.ExpiresAt.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Translation cache SET: {Hash}", entry.SourceHash);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}

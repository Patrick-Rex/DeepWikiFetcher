using System.Text.Json;
using DeepWikiFetcher.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 生成 _metadata.json（爬取统计）和 _index.json（文档目录树索引）。
/// </summary>
public sealed class OutputSerializer
{
    private readonly ILogger<OutputSerializer> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null // PascalCase，与 C# 模型一致
    };

    public OutputSerializer(ILogger<OutputSerializer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 序列化 CrawlResult 到 _metadata.json。
    /// </summary>
    public async Task WriteMetadataAsync(CrawlResult result, string outputDir, CancellationToken ct = default)
    {
        var filePath = Path.Combine(outputDir, "_metadata.json");
        var json = JsonSerializer.Serialize(result, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, ct);
        _logger.LogInformation("Written: {FilePath}", filePath);
    }

    /// <summary>
    /// 序列化 DocumentNode 树到 _index.json。
    /// </summary>
    public async Task WriteIndexAsync(DocumentNode root, string outputDir, CancellationToken ct = default)
    {
        var filePath = Path.Combine(outputDir, "_index.json");
        var json = JsonSerializer.Serialize(root, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, ct);
        _logger.LogInformation("Written: {FilePath}", filePath);
    }
}

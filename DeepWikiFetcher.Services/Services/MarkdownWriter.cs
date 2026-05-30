using System.Text;
using System.Text.Json;
using DeepWikiFetcher.Services.Interfaces;
using DeepWikiFetcher.Shared.Models;
using Markdig;
using Microsoft.Extensions.Logging;

namespace DeepWikiFetcher.Services.Services;

/// <summary>
/// 使用 Markdig 将 HTML 转换为 Markdown 文件，按层级编号命名，包含 YAML frontmatter。
/// </summary>
public sealed class MarkdownWriter : IOutputGenerator
{
    private readonly ILogger<MarkdownWriter> _logger;

    public MarkdownWriter(ILogger<MarkdownWriter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task GenerateAsync(DocumentNode root, string outputDir, CancellationToken ct = default)
    {
        _logger.LogInformation("Markdown output generation start: dir={OutputDir}", outputDir);

        // 确保输出目录存在
        Directory.CreateDirectory(outputDir);

        // 递归写入所有页面
        foreach (var child in root.Children)
        {
            WriteNodeRecursive(child, outputDir);
        }

        _logger.LogInformation("Markdown output generation complete");
        return Task.CompletedTask;
    }

    private void WriteNodeRecursive(DocumentNode node, string outputDir)
    {
        if (string.IsNullOrEmpty(node.Url) || node.Depth == 0)
        {
            // 跳过无 URL 的节点，继续处理子节点
            foreach (var child in node.Children)
            {
                WriteNodeRecursive(child, outputDir);
            }
            return;
        }

        var slug = GenerateSlug(node.Title);
        var fileName = $"{node.Number}-{slug}.md";
        var filePath = Path.Combine(outputDir, fileName);

        // 生成 YAML frontmatter + Markdown 内容
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(node.Title)}\"");
        sb.AppendLine($"url: {node.Url}");
        sb.AppendLine($"depth: {node.Depth}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(node.Content ?? $"*Content will be fetched from: {node.Url}*");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        _logger.LogInformation("Written: {FilePath}", filePath);

        foreach (var child in node.Children)
        {
            WriteNodeRecursive(child, outputDir);
        }
    }

    /// <summary>
    /// 根据英文标题生成 URL slug。
    /// 算法：小写 → 非字母数字字符替换为连字符 → 连续连字符折叠 → 首尾连字符修剪。
    /// </summary>
    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrEmpty(title))
            return "untitled";

        var sb = new StringBuilder();
        foreach (var c in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == ' ')
            {
                sb.Append(c == ' ' ? '-' : c);
            }
            else
            {
                sb.Append('-');
            }
        }

        var slug = sb.ToString();

        // 折叠连续连字符
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        // 修剪首尾连字符
        slug = slug.Trim('-');

        return string.IsNullOrEmpty(slug) ? "untitled" : slug;
    }

    private static string EscapeYaml(string value)
    {
        return value.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}

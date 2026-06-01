using DeepWikiFetcher.Shared.Models;
using DeepWikiFetcher.Shared.Enums;

namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 输出生成器：根据格式生成最终的文档文件。
/// </summary>
public interface IOutputGenerator
{
    /// <summary>支持的输出格式。</summary>
    OutputFormat Format { get; }

    /// <summary>
    /// 根据文档目录树生成输出文件。
    /// </summary>
    /// <param name="root">文档目录树根节点</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="ct">取消令牌</param>
    Task GenerateAsync(DocumentNode root, string outputDir, CancellationToken ct = default);
}

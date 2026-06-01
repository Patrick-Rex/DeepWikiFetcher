using DeepWikiFetcher.Shared.Models;

namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 翻译服务接口。
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// 批量翻译文档树中的所有节点。
    /// </summary>
    /// <param name="root">文档目录树根节点。</param>
    /// <param name="progress">进度报告回调。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>翻译后的文档树。</returns>
    Task<DocumentNode> TranslateBatchAsync(
        DocumentNode root,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
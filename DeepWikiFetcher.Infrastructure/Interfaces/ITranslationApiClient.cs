namespace DeepWikiFetcher.Infrastructure.Interfaces;

/// <summary>
/// OpenAI 兼容翻译 API 客户端接口。
/// </summary>
public interface ITranslationApiClient
{
    /// <summary>
    /// 发送翻译请求。
    /// </summary>
    /// <param name="sourceText">待翻译的 Markdown 原文。</param>
    /// <param name="model">模型名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>翻译后的 Markdown 文本；失败时返回原文。</returns>
    Task<string> TranslateAsync(string sourceText, string model, CancellationToken cancellationToken = default);
}
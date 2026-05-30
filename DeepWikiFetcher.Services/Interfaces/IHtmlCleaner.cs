using DeepWikiFetcher.Shared.Models;

namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 清洗 DeepWiki 页面 HTML，移除导航/页脚，提取图片引用。
/// </summary>
public interface IHtmlCleaner
{
    /// <summary>
    /// 清洗原始 HTML，移除非文档内容并提取图片引用。
    /// </summary>
    /// <param name="rawHtml">原始 HTML 字符串</param>
    /// <param name="baseUrl">页面基础 URL（用于修复相对链接）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>清洗结果</returns>
    Task<CleanResult> CleanAsync(string rawHtml, string baseUrl, CancellationToken ct = default);
}

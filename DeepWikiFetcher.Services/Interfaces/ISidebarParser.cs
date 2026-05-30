using DeepWikiFetcher.Shared.Models;

namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 解析 DeepWiki 页面侧边栏，构建文档目录树。
/// </summary>
public interface ISidebarParser
{
    /// <summary>
    /// 解析 DeepWiki 首页侧边栏，生成 DocumentNode 目录树。
    /// </summary>
    /// <param name="deepWikiHomeUrl">DeepWiki 首页 URL</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>文档目录树根节点</returns>
    Task<DocumentNode> ParseAsync(string deepWikiHomeUrl, CancellationToken ct = default);
}

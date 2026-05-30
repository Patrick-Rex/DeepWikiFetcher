namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 将 GitHub URL 映射为 DeepWiki URL。
/// </summary>
public interface IUrlTransformer
{
    /// <summary>
    /// 将 GitHub URL 转换为对应的 DeepWiki URL。
    /// </summary>
    /// <param name="githubUrl">GitHub 仓库 URL</param>
    /// <returns>DeepWiki URL</returns>
    /// <exception cref="ArgumentException">URL 格式不合法时抛出</exception>
    string Transform(string githubUrl);
}

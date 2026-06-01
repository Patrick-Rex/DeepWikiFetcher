# Contract: IAssetDownloader

**Layer**: Infrastructure
**Namespace**: `DeepWikiFetcher.Infrastructure.Interfaces`

## Interface Definition

```csharp
namespace DeepWikiFetcher.Infrastructure.Interfaces;

/// <summary>
/// 图片资源下载器接口。下载 DeepWiki 文档中的图片到本地 assets/images/ 目录。
/// </summary>
public interface IAssetDownloader
{
    /// <summary>
    /// 批量下载图片资源。
    /// </summary>
    /// <param name="imageUrls">HtmlCleaner 提取的图片 URL 列表</param>
    /// <param name="outputRoot">输出根目录（assets/images/ 在其下创建）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载结果列表（OriginalUrl + LocalFileName + Downloaded 状态）</returns>
    Task<List<AssetInfo>> DownloadAsync(
        List<string> imageUrls,
        string outputRoot,
        CancellationToken cancellationToken = default);
}
```

## Behavior Contract

### Download Rules

| Rule | Description |
|------|-------------|
| 域名过滤 | 仅下载 DeepWiki 域名（`*.deepwiki.com`）的图片，外部 CDN 跳过 |
| 去重 | 同一 URL 只下载一次（通过 SHA256 命名天然去重） |
| 命名 | `SHA256(original_url) + 原始扩展名`（如 `a1b2c3...f.png`） |
| 输出路径 | `{outputRoot}/assets/images/{filename}` |

### Error Handling

| Scenario | Behavior |
|----------|----------|
| 图片 404 | `AssetInfo.Downloaded = false`，记录 Warning，不阻塞 |
| 网络超时 | 重试 1 次，仍失败则 `Downloaded = false` |
| 磁盘空间不足 | 记录 Error，停止后续下载，返回已完成列表 |
| `cancellationToken` 触发 | 停止下载，返回已完成列表 |
| `imageUrls` 为空 | 立即返回空列表 |

## Dependencies

- `IHttpClientFactory` (built-in)
- Polly pipeline (复用 `IPollyPipeline`)

## Constraints

- MUST NOT 阻塞主爬取流程
- 下载失败 MUST 在 HTML/Markdown 中保留原始远程 URL 作为 fallback
- 图片总数 ≤ 200 张

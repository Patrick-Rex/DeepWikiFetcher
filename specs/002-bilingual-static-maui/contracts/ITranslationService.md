# Contract: ITranslationService

**Layer**: Services
**Namespace**: `DeepWikiFetcher.Services.Interfaces`

## Interface Definition

```csharp
namespace DeepWikiFetcher.Services.Interfaces;

/// <summary>
/// 翻译服务接口。遍历 DocumentNode 树批量翻译，管理缓存和代码保护。
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// 批量翻译文档树中的所有节点。
    /// </summary>
    /// <param name="root">文档目录树根节点（Content 已由爬取阶段填充）</param>
    /// <param name="progress">进度报告回调，每完成一个节点报告一次</param>
    /// <param name="cancellationToken">取消令牌（支持优雅停止）</param>
    /// <returns>翻译后的文档树（原始树 TransaltedTitle + TranslatedContent 已填充）</returns>
    Task<DocumentNode> TranslateBatchAsync(
        DocumentNode root,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

## Behavior Contract

### Pipeline (per node)

```
1. 检查 TranslationOptions.Enabled → false 则直接返回
2. 遍历节点（BFS/DFS 顺序，推荐 DFS 以利用局部缓存）并按 `TranslationOptions.BatchSize` 分批（默认 10 页/批）
3. 对每个批次内的节点：
   a. Content 为 null → 跳过
   b. 查询 translation_cache：SHA256(Content) → 命中则填充 TranslatedContent + TranslatedTitle
   c. 未命中：
      - 提取 Title → 翻译 → 填充 TranslatedTitle
      - 扫描 Content → 占位符替换代码块/URL → 调用 ITranslationApiClient.TranslateAsync()
      - 还原占位符 → 填充 TranslatedContent
      - 写入 translation_cache
   d. 报告 CrawlProgress
4. 返回填充后的 root
```

### Translation Protection Rules

| Content Type | Protection |
|--------------|-----------|
| 代码块 ` ```...``` ` | 替换为 `__CODE_BLOCK_{N}__` |
| 行内代码 `` `...` `` | 替换为 `__INLINE_CODE_{N}__` |
| URL `http(s)://...` | 替换为 `__URL_{N}__` |
| HTML 标签 `<...>` | 保留不替换（模型通常不翻译） |
| YAML frontmatter 元数据 | `url`、`depth` 字段保留，`title` 翻译 |

### Error Handling

| Scenario | Behavior |
|----------|----------|
| API 调用失败（重试耗尽） | 当前节点保留原文，记录 Warning 日志，继续下一节点 |
| 缓存写入失败 | 记录 Error 日志，不阻塞流程 |
| `cancellationToken` 触发 | 完成当前节点后停止，返回已翻译部分 |
| 节点 Content 为 null | 跳过该节点，记录 Warning 日志 |

## Dependencies

- `ITranslationApiClient` (Infrastructure)
- `ICacheManager` (Infrastructure, for SQLite cache read/write)
- `TranslationOptions` (Shared, for Enabled/BatchSize/MaxConcurrency check)

## Constraints

- MUST NOT 翻译代码块、行内代码、URL、HTML 属性
- MUST 保持 Markdown 结构完全不变
- 单节点失败 MUST NOT 中断整体批次
- 翻译批大小 MUST 受 `TranslationOptions.BatchSize` 控制，默认 10 页/批
- 翻译并发数 MUST 受 `TranslationOptions.MaxConcurrency` 控制

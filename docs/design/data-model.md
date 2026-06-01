---
title: "数据模型设计"
updated: "2026-06-01"
---

# 数据模型设计

## 概览

DeepWikiFetcher 的共享数据模型位于 `DeepWikiFetcher.Shared` 项目。Shared 层不包含业务逻辑，模型仅用于跨 Host、Desktop、Services 和 Infrastructure 层传递结构化数据。

## 爬取模型

### DocumentNode

`DocumentNode` 表示 DeepWiki 文档目录树中的一个节点。节点支持递归子节点，并在爬取、翻译和输出阶段逐步填充内容。

| 字段 | 类型 | 说明 |
|------|------|------|
| `Title` | `string` | 原始标题。 |
| `TranslatedTitle` | `string?` | 翻译后的标题。 |
| `Url` | `string` | 页面绝对 URL。 |
| `Depth` | `int` | 层级深度，根节点为 0。 |
| `Number` | `string` | 层级编号，例如 `1.2`。 |
| `Children` | `List<DocumentNode>` | 子节点集合。 |
| `Content` | `string?` | 清洗后的原始内容。 |
| `TranslatedContent` | `string?` | 翻译后的内容。 |

### CrawlOptions

`CrawlOptions` 是入口层传入服务层的运行参数。

| 字段 | 类型 | 说明 |
|------|------|------|
| `GitHubUrl` | `string` | GitHub 仓库 URL。 |
| `OutputRoot` | `string` | 输出根目录。 |
| `OutputFormat` | `OutputFormat` | 输出格式。 |
| `TranslationEnabled` | `bool` | 是否启用翻译阶段。 |

### CrawlResult

`CrawlResult` 是爬取完成后的统计结果。

| 字段 | 类型 | 说明 |
|------|------|------|
| `RepoKey` | `string` | 仓库标识，格式为 `owner/repo`。 |
| `TotalPages` | `int` | 总页面数。 |
| `SuccessCount` | `int` | 成功页面数。 |
| `FailCount` | `int` | 失败页面数。 |
| `Duration` | `TimeSpan` | 总耗时。 |
| `OutputPath` | `string` | 输出路径。 |

### CrawlProgress

`CrawlProgress` 供 CLI 和 MAUI 实时显示进度。

| 字段 | 类型 | 说明 |
|------|------|------|
| `Phase` | `CrawlPhase` | 当前流水线阶段。 |
| `TotalPages` | `int` | 总页面数。 |
| `CompletedPages` | `int` | 已完成页面数。 |
| `CurrentPageTitle` | `string?` | 当前页面标题。 |
| `LogMessage` | `string?` | 日志消息。 |
| `LogLevel` | `LogLevel` | 日志级别。 |

## 缓存模型

### PageCacheEntry

`PageCacheEntry` 对应 SQLite `page_cache` 表。

| 字段 | 类型 | 说明 |
|------|------|------|
| `UrlHash` | `string` | URL 的 SHA256 哈希。 |
| `Url` | `string` | 原始 URL。 |
| `Content` | `string` | 缓存的 HTML 内容。 |
| `CachedAt` | `DateTime` | 缓存时间。 |
| `ExpiresAt` | `DateTime` | 过期时间。 |

### TranslationCacheEntry

`TranslationCacheEntry` 对应 SQLite `translation_cache` 表。

| 字段 | 类型 | 说明 |
|------|------|------|
| `SourceHash` | `string` | 原文 SHA256 哈希。 |
| `PageUrl` | `string` | 页面 URL。 |
| `SourceText` | `string` | 原始 Markdown 正文。 |
| `TranslatedText` | `string` | 翻译后的 Markdown 正文。 |
| `Model` | `string` | 翻译模型标识。 |
| `CachedAt` | `DateTimeOffset` | 缓存时间。 |
| `ExpiresAt` | `DateTimeOffset` | 过期时间。 |

### CrawlMetadata

`CrawlMetadata` 对应 SQLite `crawl_metadata` 表。

| 字段 | 类型 | 说明 |
|------|------|------|
| `RepoKey` | `string` | 仓库标识。 |
| `StartedAt` | `DateTime` | 开始时间。 |
| `CompletedAt` | `DateTime?` | 完成时间。 |
| `Status` | `string` | 状态。 |
| `TotalPages` | `int` | 总页面数。 |
| `SuccessPages` | `int` | 成功页面数。 |
| `FailedPages` | `int` | 失败页面数。 |

## 输出模型

### AssetInfo

`AssetInfo` 描述资源下载结果。

| 字段 | 类型 | 说明 |
|------|------|------|
| `OriginalUrl` | `string` | 原始资源 URL。 |
| `LocalFileName` | `string` | 本地文件名。 |
| `Downloaded` | `bool` | 是否下载成功。 |

### SidebarEntry

`SidebarEntry` 用于生成 VuePress 兼容 `sidebar.json`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `Title` | `string` | 显示标题。 |
| `Path` | `string` | 页面路径。 |
| `Children` | `List<SidebarEntry>?` | 子条目。 |

### StaticSiteConfig

`StaticSiteConfig` 用于生成静态站点 `config.js`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `SiteTitle` | `string` | 站点标题。 |
| `DefaultLanguage` | `string` | 默认语言。 |
| `AvailableLanguages` | `List<string>` | 可用语言列表。 |
| `RepoKey` | `string` | 仓库标识。 |

## 枚举

### OutputFormat

`OutputFormat` 控制输出生成器选择。

| 成员 | 说明 |
|------|------|
| `Markdown` | 生成 Markdown 文档集。 |
| `StaticSite` | 生成静态 HTML 文档站。 |

### CrawlPhase

`CrawlPhase` 表示爬取流水线阶段。

| 成员 | 说明 |
|------|------|
| `UrlTransform` | URL 转换阶段。 |
| `SidebarParse` | 侧边栏解析阶段。 |
| `PageFetch` | 页面获取阶段。 |
| `HtmlClean` | HTML 清洗阶段。 |
| `AssetDownload` | 资源下载阶段。 |
| `Translation` | 翻译阶段。 |
| `Output` | 输出生成阶段。 |
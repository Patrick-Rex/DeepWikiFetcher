# Implementation Plan: DeepWikiFetcher 项目骨架与 Markdown 爬取流水线

**Branch**: `001-markdown-crawl-pipeline` | **Date**: 2026-05-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-markdown-crawl-pipeline/spec.md`

> **⚠️ PREREQUISITE**: This plan aligns with the architectural constraints defined in
> `docs/design/architecture.md`: layered architecture (Host → Services → Infrastructure → Shared),
> service boundaries, dual-mode content fetching, concurrency model, and component contracts.

## Summary

本特性为 DeepWikiFetcher 搭建 5 项目骨架并实现完整的单语言（英文）Markdown 爬取流水线。
CLI 入口接收 GitHub URL，经过 URL 转换 → 侧边栏解析 → 页面并发下载 → HTML 清洗 → Markdown
输出，生成结构化文档集。采用 Polly 弹性管道保障爬取稳定性，SQLite 缓存支持增量爬取。
MAUI 桌面端仅搭建 Shell 导航框架（三个空页面占位）。翻译、双语输出、静态站点生成均延后至 v2。

## Technical Context

**Language/Version**: C# / .NET 10
**Primary Dependencies**: AngleSharp (HTML 解析), Markdig (HTML→Markdown), Polly + Polly.Extensions (弹性管道), Microsoft.Data.Sqlite (缓存), System.CommandLine (CLI 参数), Microsoft.Extensions.* (DI/配置/日志/HttpClient)
**Storage**: SQLite (`cache.db`，输出根目录自动创建)
**Testing**: 无测试任务（spec 未要求）
**Target Platform**: Windows (CLI + MAUI 桌面端)，MAUI 多目标 (iOS/macCatalyst/Android/Windows)
**Project Type**: CLI 工具 + 桌面应用（类库 Shared/Infrastructure/Services）
**Performance Goals**: 首次爬取 50 页 ≤ 5 分钟；增量爬取（缓存命中率 > 80%）≤ 首次耗时的 20%
**Constraints**: Nullable 启用, 文件范围命名空间, 分层依赖禁止反向引用, Playwright 默认禁用
**Scale/Scope**: v1 仅英文 Markdown 输出；v2 增加翻译 + 双语 + 静态站点

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 宪法条款 | 状态 | 说明 |
|----------|------|------|
| I. 关注点分离 (5 项目) | ✅ PASS | Host/Desktop→Services→Infrastructure→Shared |
| II. 面向接口编程 | ✅ PASS | 每个 public 类有对应接口，DI 注入 |
| III. 双模式内容获取 | ⚠️ DEVIATION | Playwright 回退条件收窄为仅 HTTP 失败（非 2xx/异常），SPA 空壳检测延后至 v2（见复杂度追踪） |
| IV. 最小依赖 | ✅ PASS | Playwright 默认禁用 |
| V. 幂等设计 (SQLite) | ✅ PASS | URL SHA256 键 + 24h 过期 |
| VI. 配置驱动 | ✅ PASS | IOptions<T> 绑定 appsettings.json |
| VII. 双语输出 | ⚠️ DEVIATION | v1 仅单语言英文，`zh-cn/` 和翻译管道延后至 v2（见复杂度追踪） |
| VIII. 双输出格式 | ✅ PASS | OutputFormat 枚举已定义，v1 仅实现 MarkdownWriter |
| IX. OpenAI 翻译 | ⚠️ DEVIATION | TranslationOptions 为占位配置类，ITranslationApiClient 不实现（见复杂度追踪） |
| X. 多入口架构 | ✅ PASS | CLI + MAUI 双入口 |
| XI. Git 安全 | ✅ PASS | .gitignore 排除敏感文件，template 提交 |

## Project Structure

### Documentation (this feature)

```text
specs/001-markdown-crawl-pipeline/
├── spec.md              # 功能规格（已澄清）
├── plan.md              # 本文件
├── tasks.md             # 55 个实现任务
└── checklists/
    └── requirements.md  # 质量检查清单
```

### Source Code (repository root)

```text
DeepWikiFetcher.slnx
├── DeepWikiFetcher.Shared/          # 数据模型、枚举、配置类（无外部依赖）
│   ├── Enums/
│   │   └── OutputFormat.cs
│   ├── Models/
│   │   ├── DocumentNode.cs
│   │   ├── CrawlOptions.cs
│   │   ├── CrawlResult.cs
│   │   ├── CleanResult.cs
│   │   ├── PageCacheEntry.cs
│   │   └── CrawlMetadata.cs
│   └── Options/
│       ├── CrawlerOptions.cs
│       ├── PlaywrightOptions.cs
│       └── TranslationOptions.cs
├── DeepWikiFetcher.Infrastructure/   # 外部依赖封装：SQLite、Polly、HttpClient
│   ├── Interfaces/
│   │   ├── ICacheManager.cs
│   │   └── IPollyPipeline.cs
│   └── Services/
│       ├── CacheManager.cs
│       └── PollyPipeline.cs
├── DeepWikiFetcher.Services/         # 核心业务逻辑
│   ├── Interfaces/
│   │   ├── IUrlTransformer.cs
│   │   ├── ISidebarParser.cs
│   │   ├── IPageFetcher.cs
│   │   ├── IHtmlCleaner.cs
│   │   ├── ICrawlOrchestrator.cs
│   │   └── IOutputGenerator.cs
│   └── Services/
│       ├── UrlTransformer.cs
│       ├── SidebarParser.cs
│       ├── PageFetcher.cs
│       ├── HtmlCleaner.cs
│       ├── CrawlOrchestrator.cs
│       ├── MarkdownWriter.cs
│       └── OutputSerializer.cs
├── DeepWikiFetcher.Host/            # CLI 入口
│   ├── Program.cs
│   └── appsettings.template.json
└── DeepWikiFetcher.Desktop/         # MAUI 桌面端入口
    ├── App.xaml / App.xaml.cs
    ├── AppShell.xaml / AppShell.xaml.cs
    ├── MauiProgram.cs
    └── Pages/
        ├── SettingsPage.xaml / .cs
        ├── CrawlPage.xaml / .cs
        └── HistoryPage.xaml / .cs
```

**Structure Decision**: 采用 5 项目分层架构（Constitution I）。Shared 在最底层（零依赖），Infrastructure 仅依赖 Shared，Services 依赖 Infrastructure，Host/Desktop 依赖 Services。无循环依赖。

## Complexity Tracking

> 以下偏离项均为 v1 阶段性简化，v2 将恢复完整 Constitution 合规。

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| III: Playwright 回退仅 HTTP 失败，不含内容完整性检测 | v1 MVP 聚焦 HTTP 状态码驱动回退，SPA 空壳检测需要 JS 执行/内容长度启发式算法，增加复杂度且可在 v2 迭代 | v1 不做内容检测，用户如遇 SPA 页面可手动启用 Playwright 开关 |
| VII: 双语输出推迟 | v1 仅英文单语言，翻译管道（OpenAI API 调用、双语目录、语言切换）为独立大功能，需单独特性迭代 | 先交付单语言 MVP 验证核心技术流水线，翻译在 v2 作为独立特性实现 |
| IX: 翻译 API 占位 | v1 不调用任何翻译 API，ITranslationApiClient 接口留空，TranslationOptions 仅作配置占位 | 翻译依赖双语输出（Constitution VII），v1 无翻译场景 |

> **恢复计划**: v2 特性 `002-bilingual-translation` 将同时恢复 III（SPA 检测）、VII（双语输出）、IX（翻译 API）三项合规。

# Implementation Plan: 中英双语翻译、静态站点输出与 MAUI 桌面端

**Branch**: `002-bilingual-static-maui` | **Date**: 2026-05-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-bilingual-static-maui/spec.md`

> **⚠️ PREREQUISITE — READ FIRST**: Architecture document at `docs/design/architecture.md`
> defines layered architecture (Host/Desktop → Services → Infrastructure → Shared),
> service boundaries, dual-mode content fetching, concurrency model, and component contracts.

## Summary

在现有 001 阶段 Markdown 爬取流水线基础上，新增三大功能模块：
1. **翻译管道**：OpenAI 兼容 API 英→中翻译，SQLite 缓存去重，保护代码块/URL 不被翻译
2. **静态站点生成**：从 Markdown 中间格式生成纯静态 HTML 站点，VuePress 兼容侧边栏
3. **MAUI 桌面端**：MVVM 图形界面，设置/抓取/历史三页，Preferences + SecureStorage 配置

技术方案：在 Infrastructure 层新增 TranslationApiClient + AssetDownloader，Services 层新增 TranslationService + StaticSiteGenerator，Desktop 层实现 ViewModels 和完整 UI，Markdown 保持为主输出格式。

## Technical Context

**Language/Version**: C# 12 / .NET 10  
**Primary Dependencies**: AngleSharp (HTML 解析), Markdig (Markdown 处理), Microsoft.Data.Sqlite (SQLite), Polly (弹性), Microsoft.Maui (Desktop), System.Text.Json, IHttpClientFactory  
**Storage**: SQLite (`cache.db` 位于输出根目录) — 三表：`page_cache`、`crawl_metadata`、`translation_cache`  
**Testing**: xUnit (当前无测试覆盖，计划中)  
**Target Platform**: Windows (CLI 跨 .NET 支持平台，MAUI 目标 `net10.0-windows10.0.19041.0`)  
**Project Type**: CLI 工具 + MAUI 桌面应用（5 项目分层架构）  
**Performance Goals**: 翻译 1000 词文档 ≤15s（含 API 往返），缓存命中 ≤0.1s；MAUI 启动 ≤3s  
**Constraints**: 静态站点完全离线可浏览，CSS/JS ≤50KB；翻译 API Key 在 MAUI 端 SecureStorage 加密  
**Scale/Scope**: 单仓库 200+ 页面文档集，翻译缓存 30 天，3 个 MAUI 页面

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | 关注点分离 (5 项目分层) | ✅ PASS | 新增组件严格按层分配：TranslationApiClient/AssetDownloader → Infrastructure，TranslationService/StaticSiteGenerator → Services，ViewModels → Desktop |
| II | 面向接口编程 | ✅ PASS | 新增 ITranslationApiClient、ITranslationService、IAssetDownloader 接口，DI 注册 |
| III | 双模式内容获取 | ✅ PASS | 不修改现有 IPageFetcher 双模式逻辑 |
| IV | 最小依赖原则 | ✅ PASS | 翻译 API 使用内置 HttpClientFactory，无新增 NuGet；静态站点纯 HTML/CSS/JS 无前端框架 |
| V | 幂等设计 (SQLite 缓存) | ✅ PASS | translation_cache 表，SHA256 整页缓存键，默认 30 天过期，按 model 区分 |
| VI | 配置驱动 | ✅ PASS | TranslationOptions 已有占位需完善；MAUI Preferences + SecureStorage；appsettings.template.json 更新；翻译 BatchSize 默认 10 |
| VII | 双语输出 | ✅ PASS | zh-cn/ + en/ 分语言目录，共享 assets/images/，index.html 语言入口 |
| VIII | 双输出格式 | ✅ PASS | StaticSiteGenerator 实现 IOutputGenerator，与 MarkdownWriter 并列；OutputFormat 枚举已有 |
| IX | OpenAI 兼容翻译 | ✅ PASS | /v1/chat/completions 接口，批量化，缓存持久化，翻译失败不中断批次 |
| X | 多入口架构 | ✅ PASS | CLI 和 MAUI 共享 ICrawlOrchestrator；IProgress<CrawlProgress> 统一进度报告 |
| XI | Git 安全策略 | ✅ PASS | appsettings.json 不入库；template 更新含翻译配置；cache.db 不入库 |

**GATE RESULT**: ALL PASS — 无违规项，无需复杂性权衡记录。

## Project Structure

### Documentation (this feature)

```text
specs/002-bilingual-static-maui/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: technical research & decisions
├── data-model.md        # Phase 1: entity definitions & DTOs
├── quickstart.md        # Phase 1: developer quickstart guide
├── contracts/           # Phase 1: interface contracts
│   ├── ITranslationApiClient.md
│   ├── ITranslationService.md
│   ├── IAssetDownloader.md
│   └── IOutputGenerator.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root) — changes for this feature

```text
DeepWikiFetcher.Shared/          # [MODIFIED] 新增模型 + 完善 Options
├── Models/
│   ├── DocumentNode.cs          # [EXISTING] 可能需新增 TranslatedContent 字段
│   ├── CrawlProgress.cs         # [NEW] 进度报告 DTO
│   ├── TranslationCacheEntry.cs # [NEW] 翻译缓存记录
│   ├── AssetInfo.cs             # [NEW] 图片资源信息
│   ├── SidebarEntry.cs          # [NEW] 侧边栏条目（VuePress 兼容）
│   └── StaticSiteConfig.cs      # [NEW] 静态站点配置
├── Enums/
│   └── OutputFormat.cs          # [EXISTING] Markdown, StaticSite
└── Options/
    └── TranslationOptions.cs    # [MODIFIED] 完善 MaxConcurrency, BatchSize, CacheExpirationDays 等

DeepWikiFetcher.Infrastructure/   # [MODIFIED] 新增外部依赖封装
├── Interfaces/
│   ├── ITranslationApiClient.cs # [NEW] 翻译 API 客户端接口
│   └── IAssetDownloader.cs      # [NEW] 图片下载器接口
└── Services/
    ├── TranslationApiClient.cs  # [NEW] OpenAI 兼容 API 封装
    └── AssetDownloader.cs       # [NEW] 图片下载实现

DeepWikiFetcher.Services/         # [MODIFIED] 新增业务逻辑
├── Interfaces/
│   ├── ITranslationService.cs   # [NEW] 翻译服务接口
│   └── IOutputGenerator.cs      # [EXISTING] 已有 MarkdownWriter 实现
└── Services/
    ├── TranslationService.cs    # [NEW] 批量翻译 + 缓存 + 代码保护
    └── StaticSiteGenerator.cs   # [NEW] Markdown→HTML 静态站点生成

DeepWikiFetcher.Host/            # [MODIFIED] CLI 入口增强
├── Program.cs                   # [MODIFIED] 新增 --translate/--format 参数
└── appsettings.template.json    # [MODIFIED] 新增 Translation 节完整配置

DeepWikiFetcher.Desktop/         # [NEW/MODIFIED] MAUI 完整实现
├── ViewModels/
│   ├── SettingsViewModel.cs     # [NEW] 设置页 VM
│   ├── CrawlViewModel.cs        # [NEW] 抓取页 VM
│   └── HistoryViewModel.cs      # [NEW] 历史页 VM
├── Pages/
│   ├── SettingsPage.xaml/.cs    # [MODIFIED] 完整设置表单
│   ├── CrawlPage.xaml/.cs       # [MODIFIED] 进度条+日志流+控制按钮
│   └── HistoryPage.xaml/.cs     # [MODIFIED] 历史列表+打开目录
└── MauiProgram.cs               # [MODIFIED] 注册 ViewModels + Services

docs/
├── design/
│   ├── database.md              # [NEW] 完整 DDL（三表）
│   └── data-model.md            # [NEW] 所有 DTO 定义
├── README.md                    # [MODIFIED] 项目说明、快速开始
└── [existing files unchanged]
```

**Structure Decision**: 沿用现有 5 项目分层架构。新增组件严格按层分配：
- Infrastructure 负责外部 API 调用和资源下载
- Services 负责业务编排和格式转换
- Desktop 负责 MVVM UI 绑定
- Shared 负责跨层数据传输

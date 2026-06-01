# Tasks: 中英双语翻译、静态站点输出与 MAUI 桌面端

**Input**: plan.md, spec.md, research.md, data-model.md, contracts/

> **⚠️ 所有任务均需遵循 `docs/design/architecture.md` 分层架构与依赖约束。**

---

## Phase 1: Setup

- [X] T001 [P] 创建 Shared/Models/TranslationCacheEntry.cs
- [X] T002 [P] 创建 Shared/Models/AssetInfo.cs
- [X] T003 [P] 创建 Shared/Models/SidebarEntry.cs
- [X] T004 [P] 创建 Shared/Models/StaticSiteConfig.cs
- [X] T005 [P] 创建 Shared/Models/CrawlProgress.cs + CrawlPhase 枚举
- [X] T006 [P] 修改 Shared/Models/DocumentNode.cs，新增 TranslatedContent 字段
- [X] T007 [P] 修改 Shared/Models/CleanResult.cs，新增 AssetInfos 字段
- [X] T008 [P] 修改 Shared/Options/TranslationOptions.cs，补全 MaxConcurrency、BatchSize、CacheExpirationDays、RequestDelayMs
- [X] T009 [P] 创建 Shared/Enums/CrawlPhase.cs、完善 OutputFormat.cs

---

## Phase 2: Foundational

- [X] T010 [P] 创建 Infrastructure/Interfaces/ITranslationApiClient.cs
- [X] T011 [P] 创建 Infrastructure/Interfaces/IAssetDownloader.cs
- [X] T012 [P] 创建 Services/Interfaces/ITranslationService.cs
- [X] T013 [P] 创建 Services/Services/TranslationService.cs（批量翻译+缓存+代码保护）
- [X] T014 [P] 创建 Infrastructure/Services/TranslationApiClient.cs（OpenAI 兼容 API 封装）
- [X] T015 [P] 创建 Infrastructure/Services/AssetDownloader.cs（图片下载实现）
- [X] T016 [P] 创建 Services/Services/StaticSiteGenerator.cs（Markdown→HTML 静态站点生成）
- [X] T017 [P] 修改 Services/Interfaces/IOutputGenerator.cs，确保支持多实现
- [X] T018 [P] 修改 Host/Program.cs，支持 --translate/--format 参数
- [X] T019 [P] 修改 Host/appsettings.template.json，新增 Translation 节完整配置
- [X] T020 [P] 修改 ServiceCollectionExtensions，注册所有新接口实现

---

## Phase 3: User Story 1 - 一键中英双语文档翻译 (P1)

- [X] T021 [US1] 实现翻译缓存表 migration（docs/design/database.md）
- [X] T022 [US1] 完善 TranslationService，遍历 DocumentNode 树并按 TranslationOptions.BatchSize（默认 10 页/批）分批调度翻译
- [X] T023 [US1] 实现代码块/URL/行内代码保护与还原算法
- [X] T024 [US1] 实现批内并发控制，确保 MaxConcurrency 限制批次内同时翻译页面数
- [X] T025 [US1] 实现翻译缓存命中逻辑（SHA256 整页）
- [X] T026 [US1] 实现翻译失败降级与日志
- [X] T027 [US1] 完善 TranslationOptions 配置绑定与校验（含 BatchSize 范围校验）
- [X] T028 [US1] CLI/MAUI 端到端集成测试（含缓存、降级、批大小、配置）

---

## Phase 4: User Story 2 - 静态站点生成与浏览 (P2)

- [X] T029 [US2] 实现 StaticSiteGenerator 以 Markdown 为输入生成 HTML
- [X] T030 [US2] 生成 VuePress 兼容 sidebar.json
- [X] T031 [US2] 生成 config.js（站点配置）
- [X] T032 [US2] 生成根级 index.html（语言选择/跳转）
- [X] T033 [US2] 生成 en/、zh-cn/ 目录结构及页面
- [X] T034 [US2] 生成基础 style.css、sidebar.js（≤50KB）
- [X] T035 [US2] 完善图片本地化与路径替换
- [X] T036 [US2] 单语言模式下仅生成 en/ 目录，index.html 跳转
- [X] T037 [US2] 完善静态站点端到端测试（浏览器打开、语言切换、离线可用）

---

## Phase 5: User Story 3 - MAUI 桌面端图形化操作 (P3)

- [X] T038 [US3] 创建 Desktop/ViewModels/SettingsViewModel.cs
- [X] T039 [US3] 创建 Desktop/ViewModels/CrawlViewModel.cs
- [X] T040 [US3] 创建 Desktop/ViewModels/HistoryViewModel.cs
- [X] T041 [US3] 完善 SettingsPage.xaml/.cs，绑定所有配置项
- [X] T042 [US3] 完善 CrawlPage.xaml/.cs，进度条、日志流、控制按钮
- [X] T043 [US3] 完善 HistoryPage.xaml/.cs，历史列表、打开目录
- [X] T044 [US3] 修改 MauiProgram.cs，注册所有 ViewModel + Service
- [X] T045 [US3] 实现 Preferences/SecureStorage 配置持久化
- [X] T046 [US3] 实现 IProgress<CrawlProgress> 进度上报与 UI 刷新
- [X] T047 [US3] 实现暂停/取消逻辑（优雅停止/立即终止）
- [X] T048 [US3] MAUI 端到端集成测试（配置、抓取、历史、异常处理）

---

## Phase 6: Polish & Cross-Cutting

- [X] T049 [P] 完善 docs/design/database.md，DDL/索引/约束
- [X] T050 [P] 完善 docs/design/data-model.md，所有 DTO/模型定义
- [X] T051 [P] 更新 README.md，项目说明、功能列表、快速开始
- [X] T052 [P] 代码清理与注释补全
- [X] T053 [P] 性能优化与边界测试
- [X] T054 [P] 端到端全方案编译验证（dotnet build DeepWikiFetcher.slnx）

---

## Dependencies & Execution Order

- Phase 1/2 必须全部完成，才能进入各用户故事实现
- US1/US2/US3 可并行推进，互不阻塞
- Polish 阶段可与后期用户故事并行

---

## Parallel Opportunities

- 所有 [P] 标记任务可并行
- US1/US2/US3 各自可独立并行推进
- 文档、测试、优化可穿插进行

---

## MVP Scope

- Phase 1/2 + US1（T001-T028）为最小可用闭环
- US2/US3 可增量交付

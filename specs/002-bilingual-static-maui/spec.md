# Feature Specification: 中英双语翻译、静态站点输出与 MAUI 桌面端

**Feature Branch**: `002-bilingual-static-maui`
**Created**: 2026-05-30
**Status**: Draft
**Input**: User description: "为 DeepWikiFetcher 添加中英双语翻译、静态站点输出和 MAUI 桌面端"

> **⚠️ PREREQUISITE**: This spec aligns with the architectural constraints defined in
> `docs/design/architecture.md`: layered architecture (Host/Desktop → Services → Infrastructure → Shared),
> service boundaries, dual-mode content fetching, concurrency model, and component contracts.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 一键中英双语文档翻译 (Priority: P1)

用户爬取 GitHub 仓库的 DeepWiki 文档后，系统自动将英文原文翻译为中文，生成中英双语 Markdown 文档集。翻译过程智能跳过代码块、行内代码、URL 等不应翻译的内容，保持 Markdown 结构完全不变。翻译结果缓存至本地数据库，重复翻译不产生额外费用。

**Why this priority**: 翻译是 DeepWikiFetcher 的核心差异化能力——大多数文档抓取工具只能获取原文，而中英双语输出直接解决国内开发者阅读英文文档的语言障碍。静态站点和 MAUI 界面都依赖双语内容来体现完整价值。

**Independent Test**: 执行 `dotnet run -- --url https://github.com/owner/repo --output ./output` 且翻译配置启用，验证 `./output/{owner}/{repo}/zh-cn/` 目录下生成了中文翻译的 Markdown 文件，`en/` 目录下保留英文原文，所有 Markdown 结构（标题层级、代码块、链接、列表）在翻译前后保持一致。

**Acceptance Scenarios**:

1. **Given** 翻译已启用且配置了有效的 API Key，**When** 爬取流水线完成 HTML 清洗后，**Then** 系统遍历所有 DocumentNode，对每个节点的正文内容调用翻译 API，将翻译结果分别写入 `zh-cn/` 和 `en/` 目录。
2. **Given** 一篇包含代码块（```...```）、行内代码（\`...\`）、URL 链接和 HTML 标签的英文文档，**When** 执行翻译，**Then** 代码块内容、行内代码、URL 地址、HTML 属性值保持原样不被翻译，仅翻译自然语言文本。
3. **Given** 某段文本之前已翻译且缓存未过期，**When** 再次爬取同一仓库，**Then** 系统命中 SQLite 翻译缓存（SHA256 键），不调用翻译 API，避免重复计费。
4. **Given** 翻译 API 调用失败（如网络超时或额度耗尽），**When** 重试耗尽后，**Then** 该节点保留英文原文并记录警告日志，不影响其他节点的翻译和整体爬取流程。
5. **Given** 用户未启用翻译（`Translation.Enabled = false`），**When** 执行爬取，**Then** 系统仅生成英文原文输出，不调用翻译 API，输出目录结构与 001 阶段兼容（单层平铺）。

---

### User Story 2 - 静态站点生成与浏览 (Priority: P2)

用户可以选择输出格式为"静态站点"，系统生成一套完整的纯静态 HTML 网站，包含侧边栏导航、语言切换、基础样式和搜索功能，无需任何外部框架或构建工具，直接在浏览器中打开即可浏览。

**Why this priority**: 静态站点输出让文档可以部署到 GitHub Pages、Vercel 等静态托管服务，显著提升文档的可分享性和可访问性。它依赖 P1 的翻译能力来生成双语站点，但单语言静态站点也可独立交付价值。

**Independent Test**: 执行爬取命令并指定 `--format StaticSite`，验证输出目录包含 `index.html`（语言选择入口）、`zh-cn/index.html`、`en/index.html`、`sidebar.json`、`config.js` 及基础 CSS/JS 文件，在浏览器中打开 `index.html` 可正常浏览和切换语言。

**Acceptance Scenarios**:

1. **Given** 用户选择 `OutputFormat.StaticSite`，**When** 爬取完成，**Then** 系统生成带侧边栏导航的静态 HTML 页面，侧边栏结构与原始 DeepWiki 目录树一致。
2. **Given** 静态站点已生成且翻译已启用，**When** 用户在浏览器中打开站点，**Then** 页面根据浏览器语言自动跳转到对应语言版本，侧边栏提供语言切换按钮。
3. **Given** 静态站点已生成，**When** 用户在浏览器中浏览，**Then** 页面渲染正常，包含基础 CSS 样式（排版、代码高亮、响应式布局），不依赖任何外部 CDN 资源。
4. **Given** 输出格式为 `Markdown`（默认），**When** 爬取完成，**Then** 系统按 001 阶段逻辑生成 Markdown 文件，StaticSiteGenerator 不被调用。
5. **Given** 文档包含图片引用（仅限 DeepWiki 域名），**When** 生成静态站点，**Then** 图片被下载到 `assets/images/` 目录并以 SHA256 命名，HTML 中的图片引用替换为本地相对路径。

---

### User Story 3 - MAUI 桌面端图形化操作 (Priority: P3)

用户通过 Windows 桌面应用进行可视化配置和爬取操作，无需记忆命令行参数。应用提供设置页（配置 URL、输出目录、格式、翻译、并发等参数）、抓取页（开始/暂停/取消按钮、实时进度条、滚动日志）、历史页（查看历次爬取记录并快速打开输出目录）。

**Why this priority**: 图形界面降低了非技术用户的使用门槛，是产品从开发者工具走向大众用户的关键一步。MAUI 桌面端是服务的消费者，不包含业务逻辑，因此可以在翻译和静态站点功能稳定后再完善界面。

**Independent Test**: 启动 MAUI 桌面应用，在设置页填写 GitHub URL 和输出目录，点击"开始抓取"，验证抓取页显示实时进度条和日志流，完成后可在历史页看到记录并点击打开输出目录。

**Acceptance Scenarios**:

1. **Given** MAUI 应用首次启动，**When** 用户进入设置页，**Then** 显示 URL 输入框、输出目录选择器、输出格式下拉框（Markdown/StaticSite）、翻译开关及配置区（Base URL、API Key、Model）、并发数调节器、Playwright 开关，所有控件带有默认值。
2. **Given** 用户在设置页填写了 API Key，**When** 保存设置，**Then** API Key 通过 SecureStorage 加密存储；其他非敏感配置（URL、输出目录、格式、并发数等）通过 Preferences 存储。
3. **Given** 用户点击"开始抓取"，**When** 爬取进行中，**Then** 进度条实时更新（百分比或页数），日志区域滚动显示各阶段信息，用户可点击"暂停"暂停爬取或"取消"终止爬取。
4. **Given** 爬取已完成，**When** 用户切换到历史页，**Then** 列表显示历次爬取记录（仓库名、时间、状态、页数），点击记录可打开对应的输出目录。
5. **Given** MAUI 应用运行中，**When** 用户操作任何功能，**Then** 界面响应流畅，不出现无响应或崩溃；所有业务逻辑通过调用 Services 层接口完成，MAUI 项目自身不含业务逻辑。

---

### Edge Cases

- 翻译 API 返回格式异常（非 JSON 或缺少 choices 字段）时，系统应捕获解析异常并降级为保留原文，不中断流程。
- 用户在翻译进行中关闭 MAUI 应用，系统不做恢复提示（暂停语义为优雅停止，不保存中间状态），用户需重新发起爬取。
- 翻译 API 的 Base URL 配置了无效端点时，系统应在首次调用时就给出明确错误提示"无法连接到翻译服务"。
- 静态站点输出目录已存在时，系统应覆盖旧文件（与 Markdown 输出行为一致）。
- 文档数量极大（200+ 页面）时，静态站点侧边栏渲染不应出现性能问题（如卡顿、加载超时）。
- 图片下载失败（如 404）时，HTML 中保留原始远程 URL 作为 fallback，不阻塞站点生成。
- `assets/images/` 中不同页面的相同图片（通过 SHA256 去重）只下载一次，避免冗余存储。
- 翻译缓存数据库文件损坏时，系统应自动重建缓存表，之前的翻译结果丢失但功能不受影响。

## Requirements *(mandatory)*

### Functional Requirements

**翻译基础设施（Infrastructure 层）**：

- **FR-001**: 系统 MUST 实现 `TranslationApiClient`，封装 OpenAI 兼容 `/v1/chat/completions` 接口，支持用户配置 Base URL、API Key、Model，适配任意兼容端点（如 OpenAI、Azure OpenAI、本地 Ollama 等）。
- **FR-002**: `TranslationApiClient` MUST 通过 `IHttpClientFactory` 创建 HttpClient，遵循 Polly 弹性管道（限流 + 重试 + 熔断），配置独立于爬取管道。
- **FR-003**: 系统 MUST 在 SQLite 中创建 `translation_cache` 表，缓存键为 `SHA256(source_text)`（按整页粒度：每页的整体 Markdown 正文作为一个缓存单元），缓存值为翻译结果，支持过期策略（默认 30 天）。

**翻译服务（Services 层）**：

- **FR-004**: 系统 MUST 实现 `TranslationService`，遍历 `DocumentNode` 树批量翻译：按 `TranslationOptions.BatchSize` 对待翻译页面分批处理（默认 10 页/批），对每个节点的正文内容提取可翻译文本 → 查询缓存 → 调用 API → 写入缓存 → 填充 `TranslatedTitle` 和翻译后正文；批内并发受 `TranslationOptions.MaxConcurrency` 控制。
- **FR-005**: 翻译 MUST 保护以下内容不被翻译：代码块（\`\`\`...\`\`\`）、行内代码（\`...\`）、URL 地址（`http://...`、`https://...`）、HTML 标签和属性、图片 alt 文本中的文件名模式。
- **FR-006**: 翻译 Prompt MUST 要求保持 Markdown 结构完全不变（标题层级、列表缩进、链接引用、表格格式），仅翻译自然语言文本。
- **FR-007**: 翻译功能 MUST 通过 `TranslationOptions.Enabled` 控制开关，默认关闭。当 `Enabled = false` 时，整个翻译管道跳过，输出行为与 001 阶段兼容。

**双语输出结构**：

- **FR-008**: 输出目录 MUST 采用统一结构：始终使用 `en/` 子目录存放英文内容。双语模式（翻译启用）额外生成 `zh-cn/` 目录。共享 `assets/images/` 图片目录。根级 `index.html` 在双语模式下提供语言选择，单语模式下直接跳转到 `en/`。Markdown 输出格式在翻译关闭时保持 001 阶段平铺结构（向后兼容）。
- **FR-009**: 系统 MUST 实现 `AssetDownloader`，在 HTML 清洗后、翻译之前下载文档中引用的图片到 `assets/images/`，仅限 DeepWiki 域名的图片，以 SHA256 命名避免冲突和去重。下载完成后，HTML/Markdown 中的图片引用替换为本地相对路径。
- **FR-010**: 系统 MUST 生成根级 `index.html` 语言选择入口页面，自动检测浏览器语言（`navigator.language`）并跳转到对应语言子目录。

**静态站点输出（Services 层）**：

- **FR-011**: 系统 MUST 实现 `StaticSiteGenerator`，实现 `IOutputGenerator` 接口，与 `MarkdownWriter` 并列。通过 `OutputFormat` 枚举（`Markdown` / `StaticSite`）选择激活的实现。
- **FR-012**: `StaticSiteGenerator` MUST 以 MarkdownWriter 生成的 Markdown 文件为输入，将 Markdown 转换为纯静态 HTML 页面，不依赖任何外部前端框架（如 React、Vue）或 CDN 资源。Markdown 为主输出格式，静态站点为附加格式。
- **FR-013**: 静态站点 MUST 输出 `sidebar.json`（VuePress 兼容格式，含 `title`、`path`、`children` 字段）、`config.js`（站点标题、默认语言等配置）和基础 CSS/JS（排版、代码高亮、响应式布局、语言切换逻辑）。
- **FR-014**: 每个 HTML 页面 MUST 包含基于 `DocumentNode` 树生成的侧边栏导航和面包屑导航。

**MAUI 桌面端（Desktop 层）**：

- **FR-015**: MAUI 桌面应用 MUST 使用 MVVM 模式，Pages 仅负责 UI 渲染，ViewModels 调用 Services 层接口，MAUI 项目不包含业务逻辑。
- **FR-016**: 设置页 MUST 提供：URL 输入、输出目录选择（带文件夹浏览器）、输出格式下拉框、翻译配置区（开关、Base URL、API Key 密码框、Model 输入）、并发数调节器、Playwright 开关。
- **FR-017**: 抓取页 MUST 提供：开始/暂停/取消按钮、实时进度条（通过 `IProgress<CrawlProgress>` 报告）、可滚动日志流（Info/Warning/Error 分级颜色显示）。
- **FR-018**: 历史页 MUST 展示 `crawl_metadata` 表记录列表（仓库名、时间、状态、页数），点击记录打开输出目录（调用系统文件管理器）。
- **FR-019**: MAUI 配置持久化 MUST 使用 `Preferences` 存储非敏感配置，`SecureStorage` 加密存储 API Key。应用启动时自动加载已保存配置。
- **FR-020**: MAUI 目标平台 MUST 为 Windows（`TargetFrameworks: net10.0-windows10.0.19041.0`）。

**收尾与文档**：

- **FR-021**: 系统 MUST 完善 `docs/design/database.md`，包含 `page_cache`、`crawl_metadata`、`translation_cache` 三表的完整 DDL（含索引和约束）。
- **FR-022**: 系统 MUST 完善 `docs/design/data-model.md`，包含所有 DTO/模型定义（`DocumentNode`、`CrawlOptions`、`CrawlResult`、`CleanResult`、`PageCacheEntry`、`TranslationCacheEntry`、`CrawlProgress`、`TranslationOptions` 等）。
- **FR-023**: 系统 MUST 更新 `README.md`，包含项目说明、功能列表、快速开始指南（环境要求、配置步骤、CLI 使用示例、MAUI 启动说明）。
- **FR-024**: 系统 MUST 通过 `dotnet build DeepWikiFetcher.slnx` 全方案编译，零错误零警告（`TreatWarningsAsErrors`）。

### Key Entities

- **TranslationCacheEntry**（新增）: 翻译缓存记录（按整页粒度）。核心属性：`SourceHash`（整页原文 SHA256，主键）、`PageUrl`（页面 URL，辅助索引）、`SourceText`（整页原文）、`TranslatedText`（整页译文）、`Model`（翻译模型标识）、`CachedAt`（缓存时间）、`ExpiresAt`（过期时间，默认缓存 30 天）。
- **TranslationOptions**（已有占位，完善）: 翻译配置。核心属性：`Enabled`（默认 false）、`BaseUrl`、`ApiKey`、`Model`、`MaxConcurrency`（翻译并发数，默认 1）、`BatchSize`（批大小，默认 10 页/批）、`CacheExpirationDays`（默认 30）。
- **CrawlProgress**（新增）: 爬取进度报告。核心属性：`Phase`（当前阶段：URL 转换/侧边栏解析/页面获取/清洗/翻译/输出）、`TotalPages`、`CompletedPages`、`CurrentPageTitle`、`LogMessage`、`LogLevel`。
- **AssetInfo**（新增）: 图片资源信息。核心属性：`OriginalUrl`、`LocalFileName`（SHA256 命名）、`Downloaded`（是否下载成功）。
- **SidebarEntry**（新增）: 侧边栏条目（用于 `sidebar.json` 序列化）。核心属性：`Title`、`Path`、`Children`，与 VuePress sidebar 格式兼容。
- **StaticSiteConfig**（新增）: 静态站点配置（对应 `config.js`）。核心属性：`SiteTitle`、`DefaultLanguage`、`AvailableLanguages`、`RepoKey`。

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 翻译一篇 1000 词英文文档的耗时不超过 15 秒（含 API 往返），翻译缓存命中时耗时不超过 0.1 秒。
- **SC-002**: 翻译质量达到可读水平：翻译后的中文文档中，代码块和 URL 的误翻译率为 0%（所有代码和链接保持原样）。
- **SC-003**: 翻译缓存命中时，重复爬取同一仓库的翻译阶段耗时不超过首次翻译的 5%。
- **SC-004**: 静态站点在主流浏览器（Chrome、Edge、Firefox 最新版）中打开，页面渲染正常，语言切换功能可用。
- **SC-005**: 静态站点在无网络连接环境下，所有页面可正常浏览、导航和语言切换，不依赖任何外部在线资源。
- **SC-006**: MAUI 桌面应用从启动到显示主界面的时间不超过 3 秒，点击"开始抓取"后 UI 保持响应（不阻塞主线程）。
- **SC-007**: 用户无需阅读任何文档即可通过 MAUI 界面完成一次完整的爬取操作（从配置到查看结果）。
- **SC-008**: 全方案在本地开发环境中编译通过，零错误零警告（编译警告视为错误）。

## Clarifications

### Session 2026-05-30

- Q: 翻译缓存 `SHA256(source_text)` 的切分粒度？ → A: 按整页（每页的整体 Markdown 正文作为一个缓存单元）
- Q: MAUI "暂停"按钮的语义？ → A: 优雅停止——完成当前页面后停止，不保存中间状态，用户可重新开始
- Q: 翻译关闭时静态站点输出结构？ → A: 统一单语结构——始终使用 en/ 子目录存放英文 HTML，生成 index.html 直接跳转到 en/，保持与双语模式结构一致
- Q: StaticSiteGenerator 的输入源？ → A: 从 Markdown 中间格式转换——先经 MarkdownWriter 生成 .md，再 Markdown→HTML；Markdown 为主输出格式
- Q: AssetDownloader 的执行时机？ → A: HTML 清洗后立即下载——HtmlCleaner 提取图片引用后，AssetDownloader 在翻译之前下载所有图片，后续阶段直接使用本地路径

## Assumptions

- 用户自行获取 OpenAI 兼容 API 的 Base URL 和 API Key，DeepWikiFetcher 不提供内置的翻译服务端点。
- 翻译目标语言固定为简体中文（zh-CN），不支持其他目标语言（可后续扩展）。
- 翻译仅处理英文→中文方向，不涉及中文→英文或其他语言对。
- 静态站点的基础 CSS/JS 保持最小化（不超过 50KB 总计），不实现全文搜索、暗色模式等高级功能（可后续扩展）。
- MAUI 桌面端仅支持 Windows 平台，不包含 macOS 或 Linux 桌面支持（MAUI 理论上跨平台但当前仅验证 Windows）。
- Playwright 依赖仍由用户自行安装，MAUI 中的 Playwright 开关仅控制是否启用回退模式。
- SQLite 数据库文件统一管理在输出根目录（`{output}/cache.db`），翻译缓存与页面缓存在同一数据库的不同表中。
- 翻译 API 的限流和重试策略复用现有 Polly 管道基础设施，但使用独立的配置参数（因为翻译 API 的限流规则可能与 DeepWiki 爬取不同）。
- 现有 001 阶段的 Markdown 输出行为作为默认模式完全保留，翻译和静态站点均为可选增强。
- 项目继续遵循 `docs/design/architecture.md` 的分层架构和 `docs/spec/csharp-coding-standard.md` 的代码规范。

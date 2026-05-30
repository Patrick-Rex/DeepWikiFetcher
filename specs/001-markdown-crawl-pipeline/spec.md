# Feature Specification: DeepWikiFetcher 项目骨架与 Markdown 爬取流水线

**Feature Branch**: `001-markdown-crawl-pipeline`  
**Created**: 2026-05-30  
**Status**: Clarified  
**Input**: User description: "为 DeepWikiFetcher 搭建 5 项目骨架并实现完整单语言 Markdown 爬取流水线"

> **⚠️ PREREQUISITE**: This spec aligns with the architectural constraints defined in
> `docs/design/architecture.md`: layered architecture (Host → Services → Infrastructure → Shared),
> service boundaries, dual-mode content fetching, concurrency model, and component contracts.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - CLI 一键爬取 GitHub 仓库文档 (Priority: P1)

开发者或文档工程师在终端中输入 GitHub 仓库 URL，系统自动完成 URL 转换、侧边栏解析、页面下载、HTML 清洗和 Markdown 输出，最终在指定目录生成结构化的 Markdown 文档集。

**Why this priority**: 这是 DeepWikiFetcher 的核心价值——从 GitHub URL 到可离线浏览的文档集。没有这个流水线，其他所有功能（翻译、静态站点、MAUI 界面）都无从谈起。

**Independent Test**: 在终端执行 `dotnet run -- --url https://github.com/owner/repo --output ./output`，验证输出目录生成了完整的 Markdown 文档集（包含 `_metadata.json`、`_index.json` 和按层级编号的 `.md` 文件）。

**Acceptance Scenarios**:

1. **Given** 一个有效的 GitHub 仓库 URL（如 `https://github.com/dotnet/runtime`），**When** 用户通过 CLI 执行爬取命令，**Then** 系统输出该仓库的 DeepWiki 文档 Markdown 文件集，文件按层级编号（如 `1-getting-started.md`、`1.1-installation.md`），并生成 `_metadata.json` 和 `_index.json`。
2. **Given** 一个无效的 GitHub URL（如 `https://example.com/foo`），**When** 用户执行爬取命令，**Then** 系统输出明确的错误信息并退出，不生成任何输出文件。
3. **Given** 一个有效的 GitHub URL 且之前已成功爬取过（缓存未过期），**When** 用户再次执行爬取命令，**Then** 系统使用缓存跳过网络请求，显著加快完成速度。
4. **Given** 爬取过程中某个页面下载失败（如 404），**When** 流水线继续执行，**Then** 系统记录失败信息但不中断整体爬取，最终 `_metadata.json` 中明确区分成功和失败的页面。
5. **Given** 爬取流水线启动，**When** 流水线各阶段完成（URL 转换、侧边栏解析、页面获取、清洗、输出生成），**Then** 控制台打印 Info 级别日志标记阶段开始和完成，用户可追踪爬取进度。

---

### User Story 2 - 弹性容错保障爬取稳定性 (Priority: P2)

在网络不稳定或 DeepWiki 服务限流的情况下，系统通过内置的限流、重试和熔断机制保障爬取的稳定性和成功率，用户无需手动处理临时故障。

**Why this priority**: 弹性机制是生产级爬虫的关键差异点。没有它，网络抖动或目标服务限流都会导致爬取失败，用户体验极差。但它依赖于 P1 的流水线存在。

**Independent Test**: 模拟网络故障场景（如断开网络 10 秒后恢复），验证系统自动重试并在网络恢复后继续爬取，无需用户干预。

**Acceptance Scenarios**:

1. **Given** DeepWiki 服务返回 429（请求过多），**When** 系统检测到限流响应，**Then** 自动等待后重试，最多重试 3 次，每次等待时间指数增长。
2. **Given** 连续 5 次请求失败，**When** 熔断器触发，**Then** 系统暂停请求 30 秒后进入半开状态，尝试恢复。
3. **Given** 限流配置为每分钟 30 次，**When** 系统发送请求，**Then** 请求间隔不低于 2 秒，确保不超过速率限制。
4. **Given** 用户通过 `appsettings.json` 修改了重试次数为 5 次，**When** 爬取过程中遇到临时故障，**Then** 系统按用户配置重试最多 5 次。

---

### User Story 3 - MAUI 桌面端空壳就绪 (Priority: P3)

用户可以通过 MAUI 桌面应用看到 DeepWikiFetcher 的图形界面框架，包含设置、抓取、历史三个页面占位，为后续图形化功能开发奠定基础。

**Why this priority**: MAUI 桌面端是产品路线图中的重要入口，当前版本仅需搭建导航框架和页面占位，不包含实际业务功能。它依赖于 Shared/Infrastructure/Services 层的项目结构存在，但可以独立验证 UI 框架是否正确搭建。

**Independent Test**: 启动 MAUI 桌面应用，验证 Shell 导航栏显示三个选项卡（设置/抓取/历史），点击各选项卡可切换到对应空白页面。

**Acceptance Scenarios**:

1. **Given** MAUI 桌面应用已编译，**When** 用户启动应用，**Then** 显示带有"设置"、"抓取"、"历史"三个选项卡的 Shell 导航界面。
2. **Given** 用户在 MAUI 应用任意页面，**When** 用户点击导航栏选项卡，**Then** 页面切换到对应选项卡内容区域。

---

### Edge Cases

- 输入的 GitHub URL 指向一个没有 DeepWiki 文档的仓库时，系统应该如何响应？（应输出明确提示"该仓库暂无 DeepWiki 文档"）
- 输出目录已存在同名文件时，系统应覆盖还是提示冲突？（默认覆盖，可通过配置控制）
- 网络完全不可用时（非临时故障），系统应在所有重试耗尽后给出清晰的汇总报告，而非无限等待。
- 侧边栏解析返回空目录树时（即 DeepWiki 页面存在但无侧边栏），系统应输出空文档集并记录警告。
- 输出路径包含特殊字符或超长路径时（Windows 路径长度限制），系统应进行路径合法性校验。
- 并发爬取大量页面时（如 100+ 页面），Channel 缓冲区满不应导致死锁，生产者应等待消费者消费。

## Requirements *(mandatory)*

### Functional Requirements

**项目结构**：

- **FR-001**: 系统 MUST 包含 5 个项目：`DeepWikiFetcher.Shared`、`DeepWikiFetcher.Infrastructure`、`DeepWikiFetcher.Services`、`DeepWikiFetcher.Host`（CLI 入口）、`DeepWikiFetcher.Desktop`（MAUI 空壳）。
- **FR-002**: 项目依赖方向 MUST 遵循 Host/Desktop → Services → Infrastructure → Shared，禁止反向依赖。
- **FR-003**: 所有项目 MUST 启用 nullable 引用类型（`<Nullable>enable</Nullable>`）并使用文件范围命名空间。
- **FR-004**: 每个 public 类 MUST 有对应的接口，通过 DI 容器注入。

**Shared 层（Data）**：

- **FR-005**: Shared 层 MUST 定义 `OutputFormat` 枚举（`Markdown`、`StaticSite`）。
- **FR-006**: Shared 层 MUST 定义 `DocumentNode` 模型（含 `Title`、`TranslatedTitle`、`Url`、`Depth`、`Number`、`Children`）。
- **FR-007**: Shared 层 MUST 定义 `CrawlOptions`、`CrawlResult`、`CleanResult` 模型。
- **FR-008**: Shared 层 MUST 定义强类型配置类（`CrawlerOptions` 含 `RateLimitPerMinute`、`MaxRetryCount`、`CircuitBreakerThreshold`、`CircuitBreakerDurationSeconds`、`MaxConcurrency`、`ChannelCapacity`、`CacheExpirationHours`、`OutputFormat`；`PlaywrightOptions`；`TranslationOptions`）支持 `IOptions<T>` 绑定。

**核心流水线（Services 层）**：

- **FR-009**: 系统 MUST 实现 `UrlTransformer`，将 GitHub URL 映射为 DeepWiki URL，输入校验不合法时抛出 `ArgumentException`。
- **FR-010**: 系统 MUST 实现 `SidebarParser`，使用 AngleSharp 解析侧边栏并构建 `DocumentNode` 目录树，每个节点分配层级编号。
- **FR-011**: 系统 MUST 实现 `PageFetcher`，使用 `HttpClient` 下载页面。仅当 HTTP 返回非 2xx 或抛出异常时，若 Playwright 配置开关启用，则回退到 Playwright 兜底（默认禁用 Playwright）。
- **FR-012**: 系统 MUST 实现 `HtmlCleaner`，清洗 HTML（移除 nav/footer，保留文章内容），提取图片引用列表。
- **FR-013**: 系统 MUST 实现 `CrawlOrchestrator`，使用 `Channel<T>` 生产者-消费者模型协调并发爬取，通过 `SemaphoreSlim` 控制最大并发数（默认 3），通过 `BoundedChannelOptions` 控制通道容量（默认 100）。

**弹性与缓存（Infrastructure 层）**：

- **FR-014**: 系统 MUST 实现 Polly 弹性管道：限流（30 次/分钟）+ 重试（指数退避，最多 3 次）+ 熔断（连续 5 次失败触发 30 秒熔断）+ 降级（单页失败不中断）。
- **FR-015**: 所有弹性参数 MUST 通过 `appsettings.json` 可配置。
- **FR-016**: 系统 MUST 实现 SQLite 缓存：`page_cache` 表（URL SHA256 做键，24 小时过期）和 `crawl_metadata` 表。
- **FR-017**: 系统 MUST 支持增量爬取：缓存命中时跳过网络请求。

**输出生成（Services 层）**：

- **FR-018**: 系统 MUST 定义 `IOutputGenerator` 接口，实现 `MarkdownWriter`（使用 Markdig 将 HTML 转为 Markdown）。
- **FR-019**: Markdown 输出文件 MUST 按 `{number}-{title-slug}.md` 格式命名。Slug 算法：英文标题 → 小写 → 非字母数字字符替换为连字符 → 连续连字符折叠 → 首尾连字符修剪。文件包含 YAML frontmatter（`title`、`url`、`depth` 字段）。
- **FR-020**: 系统 MUST 生成 `_metadata.json`（直接序列化 `CrawlResult`）和 `_index.json`（直接序列化根 `DocumentNode` 树），字段名与 C# 模型一致（PascalCase）。序列化逻辑封装在 `OutputSerializer` 组件中。
- **FR-021**: 输出目录结构 MUST 为 `{output}/{owner}/{repo}/`，`_metadata.json`、`_index.json` 和所有 `.md` 文件均位于该目录下平铺。多仓库批量爬取时通过 owner/repo 路径隔离。

**CLI 入口（Host）**：

- **FR-022**: CLI MUST 支持 `dotnet run -- --url <github-url> --output <path>` 参数格式。
- **FR-023**: CLI MUST 通过 `IOptions<T>` 绑定 `appsettings.json` 配置。
- **FR-024**: CLI 启动时 MUST 输出 "DeepWikiFetcher ready" 确认信息。

**MAUI 空壳（Desktop）**：

- **FR-025**: MAUI 应用 MUST 使用 Shell 导航，包含"设置"、"抓取"、"历史"三个页面占位，每页居中显示描述性占位标签（"设置"→"配置抓取参数 — 即将推出"、"抓取"→"抓取控制面板 — 即将推出"、"历史"→"历史记录 — 即将推出"）。

**代码规范**：

- **FR-026**: `appsettings.json` 及其变体（`appsettings.Development.json`）MUST 不提交 Git，仅提交 `appsettings.template.json` 作为模板。
- **FR-027**: 所有 public API MUST 包含 XML 文档注释。
- **FR-028**: 系统 MUST 通过 `ILogger<T>` 实现结构化日志：Info 级别记录爬取里程碑（流水线阶段开始/完成、缓存命中/未命中）、Warning 级别记录非致命故障（单页下载失败、解析异常）、Error 级别记录致命错误（配置无效、输出目录不可写）。日志输出到 Console sink，日志级别通过 `appsettings.json` `Logging` 节可配置。

### Key Entities

- **DocumentNode**: 文档目录树节点。核心属性：`Title`（原始标题）、`TranslatedTitle`（翻译标题，初始为 null）、`Url`（页面绝对路径）、`Depth`（层级深度）、`Number`（层级编号如 "1.2.3"）、`Children`（子节点集合）。形成递归树结构，代表 DeepWiki 文档的完整目录。
- **CrawlOptions**: 爬取配置。核心属性：`GitHubUrl`（源 URL）、`OutputRoot`（输出根目录）、`OutputFormat`（输出格式枚举）、`TranslationEnabled`（翻译开关，本阶段始终为 false）。
- **CrawlResult**: 爬取结果。核心属性：`RepoKey`（仓库标识 `owner/repo`）、`TotalPages`、`SuccessCount`、`FailCount`、`Duration`（耗时）、`OutputPath`。
- **CleanResult**: HTML 清洗结果。核心属性：`CleanHtml`（清洗后的 HTML 字符串）、`ImageUrls`（提取的图片引用列表）。
- **PageCacheEntry**: 页面缓存记录。核心属性：`UrlHash`（URL 的 SHA256）、`Url`、`Content`（原始 HTML）、`CachedAt`（缓存时间）、`ExpiresAt`（过期时间）。
- **CrawlMetadata**: 爬取元数据。核心属性：`RepoKey`、`StartedAt`、`CompletedAt`、`Status`、`TotalPages`、`SuccessPages`、`FailedPages`。

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 用户从输入 GitHub URL 到获得完整 Markdown 文档集的端到端时间（首次爬取，无缓存）不超过 5 分钟（以 50 页文档为准）。
- **SC-002**: 增量爬取（缓存命中率 > 80%）完成时间不超过首次爬取的 20%。
- **SC-003**: 在网络临时故障（连续 3 次 503 响应）场景下，系统无需人工干预自动恢复并完成爬取，成功率不低于 95%。
- **SC-004**: 所有弹性参数（限流频率、重试次数、熔断阈值、缓存过期时间）可通过修改配置文件调整，无需重新编译。
- **SC-005**: 项目在本地开发环境中可成功构建，无编译错误和警告。
- **SC-006**: CLI 入口对无效输入（非法 URL、不存在的输出路径）的响应时间不超过 1 秒，给出明确的错误提示。

## Clarifications *(auto-resolved)*

| Question | Resolution |
|----------|------------|
| Output directory structure for single-language mode | Single-language output uses `{output}/{owner}/{repo}/` layout: `_metadata.json`, `_index.json`, and `.md` files all at the same level. No `en/` subdirectory — this keeps the structure flat for v1 while `_index.json` carries the tree hierarchy. Future bilingual mode will add `zh-cn/`/`en/` subdirectories alongside. |
| Markdown file naming convention | `{number}-{title-slug}.md` format. Slug algorithm: English title → lowercase → non-alphanumeric chars replaced with hyphens → consecutive hyphens collapsed → leading/trailing hyphens trimmed. Examples: `1-getting-started.md`, `2.1-installation-guide.md`. |
| _metadata.json & _index.json schema | Direct serialization of `CrawlResult` and `DocumentNode` respectively. `_metadata.json` mirrors `CrawlResult` fields (RepoKey, TotalPages, SuccessCount, FailCount, Duration, OutputPath). `_index.json` serializes the root `DocumentNode` tree with all children, using PascalCase field names matching the C# model. |
| Playwright fallback trigger condition | Playwright fallback triggers ONLY when HttpClient returns non-2xx status or throws an exception — AND the Playwright configuration switch is enabled. SPA empty-shell detection (short body) is deferred to a future version. |
| MAUI placeholder page content | Each page contains a centered `Label` with a descriptive placeholder message: "设置" page shows "配置抓取参数 — 即将推出", "抓取" page shows "抓取控制面板 — 即将推出", "历史" page shows "历史记录 — 即将推出". This gives users context about each page's future purpose. |
| Logging strategy | Structured logging via `ILogger<T>` with Console sink. Three log levels: Info (crawl milestones — pipeline phase start/complete, cache hit/miss), Warning (non-fatal failures — single page download error, parse exception), Error (fatal errors — invalid config, unwritable output directory). Log level configurable via `appsettings.json` `Logging` section. File sink deferred to future iteration. |

## Assumptions

- 目标用户具备基本的命令行操作能力，熟悉 `dotnet run` 命令。
- 用户拥有稳定的互联网连接，DeepWiki 服务正常可访问。
- Playwright 浏览器依赖由用户自行安装（通过配置开关控制，默认不启用）。
- SQLite 数据库文件（`cache.db`）自动创建于输出根目录，无需用户手动管理。
- 当前阶段仅实现单语言（英文原文）Markdown 输出，翻译功能和静态站点生成在后续版本实现。
- MAUI 桌面端当前仅搭建导航框架和空页面占位，不包含实际业务逻辑。
- `appsettings.template.json` 包含所有可配置项及其默认值和说明，用户在首次使用时复制为 `appsettings.json`。
- 项目遵循 `docs/design/architecture.md` 定义的分层架构和 `docs/spec/csharp-coding-standard.md` 定义的代码规范。

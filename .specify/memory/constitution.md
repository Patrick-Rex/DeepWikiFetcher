<!--
  === Sync Impact Report ===
  Version change: 1.1.0 → 2.0.0
  Modified principles:
    - I. 关注点分离: 扩展为 5 项目结构（Host + Desktop + Services + Infrastructure + Shared）
    - V. 幂等设计: 缓存存储从文件系统迁移到 SQLite
    - VI. 配置驱动: 新增敏感配置保护与模板策略
  Added sections:
    - VII. 双语输出 (Bilingual Output)
    - VIII. 双输出格式 (Dual Output Format)
    - IX. OpenAPI 兼容翻译 (OpenAI-Compatible Translation)
    - X. 多入口架构 (Multi-Entry Architecture)
    - XI. Git 安全策略 (Git Security)
  Removed sections: N/A
  Templates requiring updates:
    - .specify/templates/plan-template.md        ✅ aligned (architecture.md prerequisite present)
    - .specify/templates/spec-template.md        ✅ aligned (architecture.md prerequisite present)
    - .specify/templates/tasks-template.md       ✅ aligned (architecture.md prerequisite present)
  Follow-up TODOs:
    - 创建 DeepWikiFetcher.Shared 项目
    - 创建 DeepWikiFetcher.Infrastructure 项目
    - 创建 DeepWikiFetcher.Services 项目
    - 创建 DeepWikiFetcher.Desktop (MAUI) 项目
    - 迁移现有 Host 代码到对应分层项目
-->

# DeepWikiFetcher Constitution

## Core Principles

### I. 关注点分离 (Separation of Concerns)

项目采用 5 项目干净分层结构：

```text
DeepWikiFetcher.Host/          ← CLI 入口（Console）
DeepWikiFetcher.Desktop/       ← MAUI 桌面端入口
DeepWikiFetcher.Services/      ← 业务逻辑
DeepWikiFetcher.Infrastructure/← 外部依赖封装
DeepWikiFetcher.Shared/        ← 数据模型与配置
```

Entry Points（Host / Desktop）仅负责 DI 注册、配置加载与启动编排，
不包含任何业务逻辑。两个入口共享 Services/Infrastructure/Shared 层。

**规则**：
- Entry Points MUST NOT 包含爬虫、解析、存储、翻译等业务代码
- Services 层 MUST 不依赖具体的 Entry Point 类型
- 每个类 MUST 有单一明确的职责
- 跨层依赖 MUST 通过接口抽象，禁止直接实例化具体类

### II. 面向接口编程 (Interface-Oriented Programming)

所有服务 MUST 定义对应的 `IService` 接口，通过 DI 容器注入。
调用方仅依赖接口契约，不感知具体实现。

**规则**：
- 每个 public Service 类 MUST 有对应的接口（如 `IPageFetcher` / `PageFetcher`）
- 接口 MUST 在独立文件中定义（每个文件一个接口）
- DI 注册 MUST 在 `Program.cs` 或专用扩展方法中完成，禁止服务定位器模式

### III. 双模式内容获取 (Dual-Mode Content Fetching)

页面内容获取支持两种模式：纯 HTTP 轻量模式（优先）和 Playwright 浏览器渲染模式（兜底）。
两种模式共享 `IPageFetcher` 接口，调用方无感知切换。

**规则**：
- HTTP 模式 MUST 作为默认首选策略
- 仅在 HTTP 模式失败或内容不完整时 fallback 到 Playwright
- 两种实现 MUST 实现同一 `IPageFetcher` 接口
- Playwright 模式 MUST 通过配置开关控制启用

### IV. 最小依赖原则 (Minimal Dependency)

Playwright for .NET 作为可选依赖，默认不加载。项目 MUST 在没有 Playwright
运行时的环境下仍可正常编译和运行（仅 HTTP 模式）。

**规则**：
- Playwright 相关代码 MUST 通过条件引用或独立项目隔离
- 默认配置 MUST 禁用 Playwright 模式
- 新增第三方依赖 MUST 有明确的必要性说明

### V. 幂等设计 (Idempotent Design)

已下载的页面 MUST 做本地缓存，支持断点续爬。对同一 URL 的重复请求 MUST
返回相同结果而不重复下载。翻译结果同样 MUST 缓存。

**规则**：
- 缓存存储 MUST 使用 SQLite（`cache.db`，位于输出根目录）
- 页面缓存键：URL 的 SHA256 哈希
- 翻译缓存键：原文文本的 SHA256 哈希
- 重复请求 MUST 命中缓存，不发起网络调用
- 支持增量更新：仅爬取缓存中不存在的页面
- 翻译缓存永不过期（同一模型下），更换模型时按 model 字段区分

### VI. 配置驱动 (Configuration-Driven)

所有运行时行为参数 MUST 通过 `appsettings.json` 控制，禁止硬编码。
使用 `IOptions<T>` 强类型配置模式。MAUI 端通过 `Preferences` + `SecureStorage` 配置。

**规则**：
- 并发数、请求间隔、超时、重试次数、输出路径、User-Agent 等 MUST 可配置
- 配置类 MUST 使用 `IOptions<T>` 模式注入
- 禁止在代码中硬编码任何魔法数字或字符串常量（除空字符串、0、1 等基本常量外）
- 配置键 MUST 使用有意义的层级命名
- `appsettings.json` MUST NOT 提交到 Git（含敏感信息）
- `appsettings.template.json` MUST 提交到 Git 作为配置参考模板
- API Key 等敏感字段在 MAUI 端 MUST 使用 `SecureStorage` 加密存储

### VII. 双语输出 (Bilingual Output)

输出支持中英双语，英文为原始内容，中文通过 OpenAI 兼容 API 翻译生成。
翻译功能默认关闭，由前端控制开启。

**规则**：
- 输出结构 MUST 包含 `zh-cn/` 和 `en/` 两个语言目录
- 图片等静态资源 MUST 放在共享 `assets/images/` 目录
- `index.html` MUST 支持自动检测浏览器语言并跳转
- 代码块、行内代码、链接 URL、HTML 属性 MUST NOT 被翻译
- 翻译 MUST 调用 OpenAI 兼容 `/v1/chat/completions` 接口
- 翻译 Prompt MUST 保持 Markdown 结构和格式完全不变

### VIII. 双输出格式 (Dual Output Format)

输出格式支持 Markdown 和静态文档站两种，用户在前端选择。

**规则**：
- 两种格式 MUST 实现同一 `IOutputGenerator` 接口
- Markdown 格式：使用 Markdig 管线，输出 `.md` 文件
- 静态站点格式：输出 `.html` + `sidebar.json`，兼容 VuePress / Docusaurus
- 文件名 MUST 按层级编号：`1-installation.md`
- 每个文件 MUST 包含 YAML frontmatter（`title`、`url`、`depth`）
- `OutputFormat` 枚举在 Shared 层定义

### IX. OpenAI 兼容翻译 (OpenAI-Compatible Translation)

翻译通过类 OpenAI 接口实现，用户配置 Base URL 和 API Key。

**规则**：
- 翻译 API 调用 MUST 通过 `ITranslationApiClient` 接口
- 支持任意 OpenAI 兼容端点（OpenAI / Azure OpenAI / 本地 Ollama 等）
- 翻译 MUST 批量化处理，批大小可配置（默认 10 页/批）
- 翻译结果 MUST 持久化到 SQLite `translation_cache` 表
- 翻译失败 MUST 触发重试（最多 3 次），单条失败不中断批次
- 翻译速率 MUST 可配置（`RequestDelayMs`），避免触发 API 限流

### X. 多入口架构 (Multi-Entry Architecture)

项目提供 CLI 与 MAUI 桌面端两种入口，共享核心逻辑。

**规则**：
- CLI（`DeepWikiFetcher.Host`）：命令行参数解析 + 后台执行
- MAUI（`DeepWikiFetcher.Desktop`）：设置/抓取/历史三页 + 实时进度
- 两个入口 MUST 通过同一个 `ICrawlOrchestrator` 接口启动爬取
- MAUI 端配置通过 `Preferences` 持久化，CLI 端通过 `appsettings.json`
- 进度报告 MUST 通过 `IProgress<CrawlProgress>` 接口统一通知

### XI. Git 安全策略 (Git Security)

**规则**：
- `appsettings.json` 及所有变体（除 template）MUST 被 `.gitignore` 排除
- `Output/` 目录 MUST 被 `.gitignore` 排除
- SQLite 数据库文件（`*.db`、`*.db-shm`、`*.db-wal`）MUST 被 `.gitignore` 排除
- `appsettings.template.json` MUST 提交到仓库，包含所有可配项及默认值
- API Key 等敏感信息 MUST NOT 出现在任何提交到仓库的文件中

---

## 弹性与鲁棒性规约

### 重试策略

Transient HTTP 错误（5xx、408、429）MUST 使用 Polly 指数退避重试，最大重试 3 次。
退避间隔公式：$2^{n} \times 1\text{s}$（n 为重试次数，从 0 开始）。

### 限流

默认每分钟最多 30 次请求，请求间隔 MUST ≥ 2 秒。限流参数通过配置调整。
使用 `SemaphoreSlim` 或 Polly 速率限制器实现。

### 超时

- 单次 HTTP 请求超时：30 秒
- Playwright 页面加载超时：60 秒
- 超时后 MUST 抛出 `TimeoutException` 并触发重试/降级逻辑

### 熔断

连续失败 5 次后 MUST 触发熔断，熔断持续 30 秒。熔断期间所有请求快速失败，
不消耗网络资源。30 秒后进入半开状态，允许一次探测请求。

### 优雅降级

单个页面爬取失败 MUST NOT 中断整体流程。失败页面 MUST 记录结构化错误日志后
跳过，继续处理剩余页面。最终报告中 MUST 汇总所有失败项。

---

## 开发规范与技术约束

技术选型、版本约束及关键使用限制详见 `docs/tech-stack.md`。
Constitution 仅定义框架无关的代码规范与输出契约，技术实现细节由 tech-stack 管理。

### 代码规范

1. **命名**：PascalCase（类/方法/属性）、camelCase（参数/变量）、`_camelCase`（私有字段）
2. **Null 安全**：`<Nullable>enable</Nullable>` 启用，禁止 `!` 抑制警告，MUST 显式空检查
3. **警告管理**：禁止 `#pragma warning disable` 和 `// ReSharper disable`
4. **文档注释**：所有 public API MUST 有 XML 文档注释
5. **异步命名**：异步方法 MUST 以 `Async` 后缀结尾
6. **命名空间**：MUST 使用文件范围命名空间（`namespace DeepWikiFetcher.Services;`）
7. **注释语言**：方法和类 MUST 有标准中文注释

### 输出规约

1. 输出根目录默认为 `./Output/{owner}/{repo}/`，MUST NOT 被 Git 追踪
2. 每个仓库 MUST 输出 `_metadata.json`（爬取统计）和 `_index.json`（目录树）
3. 双语输出 MUST 按 `zh-cn/` / `en/` 分语言目录，共享 `assets/images/`
4. 文件 MUST 按侧边栏层级编号命名（如 `1.1-installation.md`）
5. 目录树结构 MUST 保留原始侧边栏层级关系
6. `index.html` MUST 支持浏览器语言自动检测跳转
7. 输出格式由 `OutputFormat` 枚举控制：`Markdown` / `StaticSite`

### 测试要求

1. 核心逻辑（URL 转换、侧边栏解析、HTML 清洗）MUST 有单元测试
2. 集成测试 MUST 覆盖"输入 GitHub URL → 输出 Markdown 文件"完整流程
3. HTTP 调用 MUST 使用 Mock Handler，禁止发起真实网络请求
4. 测试框架与工具链详见 `docs/tech-stack.md`

---

## Governance

本宪法是 DeepWikiFetcher 项目的最高开发准则，所有代码变更、架构决策和 PR
MUST 遵循上述原则。

**修订流程**：
1. 提出修订 PR，说明变更理由与影响范围
2. 经代码审查确认不与现有原则冲突
3. 更新宪法版本号并记录 `LAST_AMENDED_DATE`
4. 同步更新受影响的模板和文档

**版本策略**：
- MAJOR：原则删除或根本性重定义
- MINOR：新增原则/章节或实质性扩展
- PATCH：措辞澄清、修正、格式调整

**合规审查**：
- 所有 PR MUST 通过 Constitution Check 门禁
- 复杂性偏离 MUST 在 plan.md 中明确说明并取得批准
- 运行时开发指导参见 `.github/copilot-instructions.md`

**Version**: 2.0.0 | **Ratified**: 2026-05-29 | **Last Amended**: 2026-05-30


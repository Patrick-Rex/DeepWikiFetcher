<!--
  === Sync Impact Report ===
  Version change: [NONE] → 1.0.0 (initial ratification)
  Modified principles: N/A (initial creation)
  Added sections:
    - Core Principles (I-VI): 关注点分离, 面向接口编程, 双模式内容获取, 最小依赖, 幂等设计, 配置驱动
    - 弹性与鲁棒性规约 (Resilience & Robustness)
    - 开发规范与技术约束 (Development Standards & Technical Constraints)
  Removed sections: N/A
  Templates requiring updates:
    - .specify/templates/plan-template.md        ✅ aligned (Constitution Check gate present)
    - .specify/templates/spec-template.md        ✅ aligned (Requirements section present)
    - .specify/templates/tasks-template.md       ✅ aligned (Phase structure supports principle-driven tasks)
  Follow-up TODOs: None
-->

# DeepWikiFetcher Constitution

## Core Principles

### I. 关注点分离 (Separation of Concerns)

项目采用干净分层结构。Host 层（ASP.NET Core）仅负责 DI 注册、配置加载与启动编排，
不包含任何业务逻辑。业务逻辑分布在独立的 Service 层中，各层职责明确、边界清晰。

**规则**：
- Host 项目 MUST NOT 包含爬虫、解析、存储等业务代码
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
返回相同结果而不重复下载。

**规则**：
- 缓存策略 MUST 基于内容哈希或 URL+时间戳
- 缓存存储 MUST 使用 SQLite 或文件系统
- 重复请求 MUST 命中缓存，不发起网络调用
- 支持增量更新：仅爬取新增或变更的页面

### VI. 配置驱动 (Configuration-Driven)

所有运行时行为参数 MUST 通过 `appsettings.json` 控制，禁止硬编码。
使用 `IOptions<T>` 强类型配置模式。

**规则**：
- 并发数、请求间隔、超时、重试次数、输出路径、User-Agent 等 MUST 可配置
- 配置类 MUST 使用 `IOptions<T>` 模式注入
- 禁止在代码中硬编码任何魔法数字或字符串常量（除空字符串、0、1 等基本常量外）
- 配置键 MUST 使用有意义的层级命名

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

### 技术栈

| 组件 | 技术选型 | 约束 |
|------|----------|------|
| 运行时 | .NET 10 | MUST |
| Web 框架 | ASP.NET Core 10 | Host 层入口 |
| HTTP 客户端 | `HttpClient` + `IHttpClientFactory` | 禁止 `new HttpClient()` |
| HTML 解析 | AngleSharp | MUST |
| 浏览器渲染 | Playwright for .NET | 可选，默认禁用 |
| 弹性策略 | Polly | 重试/超时/熔断/限流 |
| 序列化 | System.Text.Json | 禁止 Newtonsoft.Json |
| Markdown | Markdig | MUST |
| 日志 | Serilog | 结构化日志，禁止 `Console.WriteLine` |
| CLI | Spectre.Console | MUST |
| 并发 | `Channel<T>` + `SemaphoreSlim` | 生产者-消费者模型 |
| 配置 | `IOptions<T>` | 强类型配置 |
| 测试 | xUnit + FluentAssertions + NSubstitute | MUST |

### 代码规范

1. **命名**：PascalCase（类/方法/属性）、camelCase（参数/变量）、`_camelCase`（私有字段）
2. **Null 安全**：`<Nullable>enable</Nullable>` 启用，禁止 `!` 抑制警告，MUST 显式空检查
3. **警告管理**：禁止 `#pragma warning disable` 和 `// ReSharper disable`
4. **文档注释**：所有 public API MUST 有 XML 文档注释
5. **异步命名**：异步方法 MUST 以 `Async` 后缀结尾
6. **命名空间**：MUST 使用文件范围命名空间（`namespace DeepWikiFetcher.Services;`）
7. **注释语言**：方法和类 MUST 有标准中文注释

### 输出规约

1. 输出根目录默认为 `./Output/{owner}/{repo}/`
2. 每个仓库 MUST 输出 `_metadata.json`（爬取统计）和 `_index.json`（目录树）
3. 文档 MUST 以 Markdown 格式保存，文件名按侧边栏层级编号（如 `1.1-installation.md`）
4. 目录树结构 MUST 保留原始侧边栏层级关系

### 测试要求

1. 核心逻辑（URL 转换、侧边栏解析、HTML 清洗）MUST 有单元测试
2. 集成测试 MUST 覆盖"输入 GitHub URL → 输出 Markdown 文件"完整流程
3. HTTP 调用 MUST 使用 Mock Handler，禁止发起真实网络请求
4. 测试框架：xUnit + FluentAssertions + NSubstitute

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

**Version**: 1.0.0 | **Ratified**: 2026-05-29 | **Last Amended**: 2026-05-29


---
title: "DeepWikiFetcher 技术选型与版本约束"
updated: "2026-05-30"
---

# DeepWikiFetcher 技术选型与版本约束

## 运行时与 SDK

| 组件 | 选型 | 最低版本 | 约束 |
|------|------|----------|------|
| .NET SDK | .NET | 10.0.100 | MUST，项目 target `net10.0` |
| Web 框架 | ASP.NET Core | 10.0 | Minimal API，Host 层入口 |

## 核心依赖

### HTTP 客户端：`HttpClient` + `IHttpClientFactory`

- **选型理由**：`IHttpClientFactory` 管理连接池生命周期，避免 Socket 耗尽
- **约束**：
  - 禁止 `new HttpClient()`，MUST 通过 `IHttpClientFactory.CreateClient(name)` 获取
  - 命名 HttpClient MUST 在 `Program.cs` 中通过 `AddHttpClient("name", ...)` 注册
  - 默认超时 30 秒，可通过配置覆盖

### HTML 解析：AngleSharp

- **选型理由**：纯 .NET 实现，CSS Selector 语法标准，无需外部运行时
- **约束**：
  - 禁止使用正则表达式解析 HTML
  - CSS Selector MUST 定义在常量类中，禁止硬编码在业务逻辑
  - 解析大文档时 MUST 使用 `HtmlParser` 流式 API，避免全量加载

### 浏览器渲染（兜底）：Playwright for .NET

- **选型理由**：当 DeepWiki 页面为纯客户端渲染时，作为 HTTP 模式的 fallback
- **约束**：
  - 默认禁用，通过 `appsettings.json` 中的开关启用
  - Playwright 依赖的浏览器 MUST 通过 `playwright.ps1 install chromium` 安装
  - 禁止在 CI 环境默认启用 Playwright 模式
  - 每次使用后 MUST 释放浏览器实例，禁止长驻内存

### 弹性策略：Polly

- **选型理由**：.NET 生态标准的弹性策略库
- **约束**：
  - 重试策略 MUST 使用 `HandleTransientHttpError()` 过滤条件
  - 指数退避间隔：`TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))`
  - 重试上限 3 次，熔断阈值 5 次连续失败，熔断持续 30 秒
  - 限流默认每分钟 30 次，间隔 ≥ 2 秒
  - 策略管道 MUST 通过 Polly 的 `PolicyWrap` 组合，顺序为：限流 → 重试 → 熔断

### 序列化：System.Text.Json

- **选型理由**：.NET 内置，零额外依赖，性能优于 Newtonsoft.Json
- **约束**：
  - 禁止引用 `Newtonsoft.Json` 包
  - JSON 序列化配置 MUST 使用 `JsonSerializerOptions` 统一管理
  - 属性命名策略 MUST 为 `JsonNamingPolicy.CamelCase`

### Markdown 处理：Markdig

- **选型理由**：.NET 生态最快的 Markdown 解析器
- **约束**：
  - 仅用于 HTML 到 Markdown 的转换，不用于渲染
  - Pipeline MUST 配置 `UseAdvancedExtensions()` 以支持表格和代码块

### 日志：Serilog

- **选型理由**：结构化日志，支持多种 Sink
- **约束**：
  - 禁止 `Console.WriteLine()` 和 `Debug.WriteLine()`
  - MUST 通过 `appsettings.json` 配置最低日志级别
  - 异常日志 MUST 包含 `Exception` 对象，禁止仅记录 `ex.Message`

### CLI 交互：Spectre.Console

- **选型理由**：提供进度条、表格、彩色输出等丰富的终端交互组件
- **约束**：
  - 进度条 MUST 用于展示爬取进度（总页面数 / 已完成数）
  - 错误输出 MUST 使用 `MarkupLine("[red]...[/]")` 标记样式

### 并发控制：`Channel<T>` + `SemaphoreSlim`

- **选型理由**：`Channel<T>` 为生产者-消费者模型提供 BCL 内置实现，无外部依赖
- **约束**：
  - 生产者（SidebarParser）写入 Channel，消费者（PageFetcher）读取 Channel
  - `SemaphoreSlim` 控制最大并发数，默认 3，可通过配置调整
  - Channel MUST 设置容量上限（`BoundedChannelOptions`），防止内存溢出

### 配置：`IOptions<T>`

- **选型理由**：.NET 内置强类型配置模式
- **约束**：
  - 配置类 MUST 以 `Options` 后缀命名（如 `CrawlerOptions`）
  - 绑定 MUST 使用 `builder.Services.Configure<TOptions>(configuration.GetSection("key"))`
  - 禁止在构造函数中直接读取 `IConfiguration` 索引器

## 测试工具链

| 组件 | 选型 | 约束 |
|------|------|------|
| 测试框架 | xUnit | MUST |
| 断言库 | FluentAssertions | 禁止使用 `Assert.*` 静态方法 |
| Mock 框架 | NSubstitute | 禁止使用 Moq（已弃用） |
| HTTP Mock | `HttpMessageHandler` Mock | 不发起真实网络请求 |

## 构建与环境

| 项目 | 要求 |
|------|------|
| 构建工具 | `dotnet build`，无额外构建脚本依赖 |
| 包管理 | NuGet，禁止使用本地 DLL 引用 |
| 目标 OS | Windows / Linux / macOS（.NET 跨平台） |
| IDE | VS Code 或 Rider，不依赖 Visual Studio 特定功能 |

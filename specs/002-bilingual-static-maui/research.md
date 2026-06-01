# Research: 中英双语翻译、静态站点输出与 MAUI 桌面端

**Date**: 2026-05-31
**Status**: Complete
**Related**: [spec.md](./spec.md) | [plan.md](./plan.md)

## 1. OpenAI 兼容 API 集成模式

**Decision**: 使用 `IHttpClientFactory` + `System.Text.Json` 封装 `/v1/chat/completions` 端点

**Rationale**:
- `IHttpClientFactory` 已由现有项目在 Host `Program.cs` 中注册，无需新增依赖
- OpenAI 兼容 API 的请求/响应 JSON 结构简单固定：`messages` 数组 → `choices[0].message.content`
- 独立 Polly 管道：限流间隔可配 (`TranslationRequestDelayMs`)，与爬取管道隔离

**Alternatives considered**:
- 使用 OpenAI 官方 SDK (`OpenAI` NuGet)：引入额外依赖，版本锁定复杂，对兼容端点支持不如直接 HTTP
- 使用 Refit：增加依赖，翻译 API 仅一个端点不需要接口生成

**Implementation Notes**:
- Prompt 使用 `system` role 设定翻译规则，`user` role 传入 Markdown 原文
- 响应解析失败 → 记录 Error 日志 → 返回原文（降级）
- 超时 30s（与爬取一致），重试 3 次

## 2. Markdown → HTML 静态站点生成

**Decision**: 使用 Markdig 的 `HtmlPipeline`（已依赖）将 Markdown 转为 HTML，注入侧边栏模板

**Rationale**:
- Markdig 已在 001 阶段引入用于 Markdown 输出，无需新增依赖
- StaticSiteGenerator 以 MarkdownWriter 输出的 `.md` 文件为输入 → Markdig 转 HTML → 注入导航模板
- 纯静态 HTML/CSS/JS，不依赖前端框架

**Alternatives considered**:
- 从 CleanResult.CleanHtml 直接生成：双路径维护（Markdown + HTML），且 001 阶段 Markdown 为主格式
- 使用 NuGet 静态站点生成器（如 Statiq）：重量级，与"IOutputGenerator 接口"约束冲突

**Static Site Assets**:
- `css/style.css`: 排版（字体、间距、代码高亮）、响应式布局（≤50KB）
- `js/sidebar.js`: 侧边栏折叠/展开、当前页高亮、语言切换（≤10KB）
- 不使用任何 CDN 资源或外部字体

## 3. VuePress 兼容 sidebar.json 格式

**Decision**: 输出如下 JSON 结构

```json
{
  "/zh-cn/": [
    {
      "title": "安装指南",
      "path": "/zh-cn/pages/1-installation.html",
      "children": [
        { "title": "Windows 安装", "path": "/zh-cn/pages/1.1-windows.html" }
      ]
    }
  ]
}
```

**Rationale**:
- VuePress sidebar 格式业界标准，兼容 Docusaurus V1 等多种静态站点框架
- 每个语言版本独立 `sidebar.json`
- `SidebarEntry` 模型在 Shared 层定义，JSON 序列化直接映射

## 4. 翻译代码保护策略

**Decision**: 翻译前用占位符替换代码块/URL，翻译后将占位符还原

**Algorithm**:
1. 扫描 Markdown 正文，提取所有代码块（```...```）、行内代码（`...`）、URL（http/https）、HTML 标签
2. 用唯一占位符替换：`__CODE_BLOCK_0__`、`__INLINE_CODE_1__`、`__URL_2__`
3. 将替换后的纯文本发送给翻译 API
4. 翻译结果中还原占位符为原始内容

**Rationale**: 基于占位符的方法比正则匹配更可靠，不会因为翻译导致代码块标记被破坏

**Alternatives considered**:
- 分段翻译（逐段落）：API 调用次数过多，成本高，且段落间上下文丢失
- Prompt 指令保护：不可靠，部分模型可能忽略指令翻译代码

## 5. SQLite translation_cache 设计

**Decision**: 整页粒度缓存，SHA256(source_text) 为主键

```sql
CREATE TABLE IF NOT EXISTS translation_cache (
    source_hash TEXT PRIMARY KEY,
    page_url    TEXT NOT NULL,
    source_text TEXT NOT NULL,
    translated_text TEXT NOT NULL,
    model       TEXT NOT NULL DEFAULT '',
    cached_at   TEXT NOT NULL,  -- ISO 8601
    expires_at  TEXT NOT NULL   -- ISO 8601, 默认 30 天
);

CREATE INDEX IF NOT EXISTS idx_translation_cache_page_url ON translation_cache(page_url);
CREATE INDEX IF NOT EXISTS idx_translation_cache_model ON translation_cache(model);
CREATE INDEX IF NOT EXISTS idx_translation_cache_expires ON translation_cache(expires_at);
```

**Rationale**:
- 整页粒度（澄清 Q1 决定）：简单可靠，缓存命中即跳过一个完整页面的翻译
- `model` 字段区分不同模型翻译结果（Constitution V 要求）
- `page_url` 辅助索引：支持按页面查询缓存状态
- 过期字段索引：支持定期清理

**Alternatives considered**:
- 段落粒度：跨页面复用率高但缓存碎片化，管理复杂（澄清中已否决）
- 标题段粒度：边界判断复杂（澄清中已否决）

## 6. MAUI MVVM + IProgress<CrawlProgress>

**Decision**: ViewModel 通过 `ICrawlOrchestrator` 启动爬取，传入 `IProgress<CrawlProgress>` 回调

**Pattern**:
```csharp
// CrawlViewModel
var progress = new Progress<CrawlProgress>(report =>
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        ProgressValue = (double)report.CompletedPages / report.TotalPages;
        LogEntries.Add(report);
    });
});
await _orchestrator.StartAsync(options, progress, cancellationToken);
```

**Rationale**:
- `IProgress<T>` 是 .NET 标准进度报告机制，无需自定义事件
- `MainThread.BeginInvokeOnMainThread` 确保 UI 线程安全更新
- `CancellationToken` 支持优雅停止（暂停）和立即取消

**Pause Implementation** (澄清 Q2):
- "暂停" = 取消 CancellationToken → CrawlOrchestrator 完成当前页面后退出
- 不保存中间状态，用户重新开始

## 7. MAUI 配置持久化策略

**Decision**:
| 配置项 | 存储方式 |
|--------|---------|
| GitHub URL, 输出目录, 格式, 并发数, Playwright 开关 | `Preferences` |
| API Key (翻译) | `SecureStorage` |
| Base URL, Model (翻译) | `Preferences`（非敏感） |

**Rationale**:
- `SecureStorage` 在 Windows 上使用 DPAPI 加密，适合 API Key
- `Preferences` 轻量键值存储，适合非敏感配置
- 应用启动时自动加载，修改后自动保存

## 8. AssetDownloader 执行时机

**Decision**: HTML 清洗后立即下载（澄清 Q5），在翻译之前

**Pipeline Order**:
```
HtmlCleaner → AssetDownloader → [TranslationService] → OutputGenerator
```

**Rationale**:
- 图片下载完成后，翻译和输出阶段直接使用本地路径，无需后续补救
- 与架构文档 Arch sequence diagram 一致：图片在清洗后、翻译前处理

**Image Naming**: SHA256(original_url).ext，去重：相同 URL 只下载一次

## Summary of Key Decisions

| # | Topic | Decision | Phase |
|---|-------|----------|-------|
| 1 | API 集成 | HttpClientFactory + System.Text.Json | Infra |
| 2 | Markdown→HTML | Markdig HtmlPipeline | Services |
| 3 | Sidebar 格式 | VuePress sidebar.json | Services |
| 4 | 翻译保护 | 占位符替换/还原 | Services |
| 5 | 翻译缓存 | SQLite, SHA256 整页, model 区分 | Infra |
| 6 | MAUI 进度 | IProgress<CrawlProgress> + MainThread | Desktop |
| 7 | 配置存储 | Preferences + SecureStorage | Desktop |
| 8 | 图片下载 | 清洗后、翻译前 | Infra |

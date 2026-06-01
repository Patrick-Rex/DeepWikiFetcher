# Contract: IOutputGenerator (StaticSiteGenerator extension)

**Layer**: Services
**Namespace**: `DeepWikiFetcher.Services.Interfaces`

## Existing Interface (from 001)

```csharp
namespace DeepWikiFetcher.Services.Interfaces;

public interface IOutputGenerator
{
    Task GenerateAsync(
        DocumentNode root,
        CrawlResult crawlResult,
        CrawlOptions options,
        CancellationToken cancellationToken = default);
}
```

> **Note**: `IOutputGenerator` 已在 001 阶段定义，由 `MarkdownWriter` 实现。本 feature 新增 `StaticSiteGenerator` 作为第二个实现。

## StaticSiteGenerator Behavior Contract

### Input

| Parameter | Source | Description |
|-----------|--------|-------------|
| `root` | `CrawlOrchestrator` | 文档树（Title/TranslatedTitle/Content/TranslatedContent 已填充） |
| `crawlResult` | `CrawlOrchestrator` | 爬取统计 |
| `options` | User config | `OutputFormat = StaticSite` 时被选择 |

### Output: Static Site Structure

```text
{outputRoot}/
├── index.html                  # 语言选择入口（自动检测浏览器语言跳转）
├── .nojekyll                   # GitHub Pages 配置
├── _metadata.json              # 爬取统计（同 Markdown）
├── zh-cn/                      # [仅翻译启用时] 中文站点
│   ├── index.html             # 中文首页（第一页）
│   ├── sidebar.json           # VuePress 兼容侧边栏
│   ├── config.js              # 站点配置
│   └── pages/
│       ├── 1-installation.html
│       └── ...
├── en/                         # 始终存在：英文站点
│   ├── index.html
│   ├── sidebar.json
│   ├── config.js
│   └── pages/
│       └── ...
└── assets/
    ├── images/                 # 共享图片（AssetDownloader 填充）
    ├── css/
    │   └── style.css           # 基础样式（≤50KB）
    └── js/
        └── sidebar.js          # 侧边栏交互 + 语言切换（≤10KB）
```

### Page Generation (per language)

```
For each language (en, zh-cn if translation enabled):
  1. Generate sidebar.json from DocumentNode tree
     - Convert DocumentNode → SidebarEntry recursive
     - path = /{lang}/pages/{number}-{slug}.html
  2. Generate config.js
     - SiteTitle = root.Title (or root.TranslatedTitle for zh-cn)
     - DefaultLanguage = lang
     - AvailableLanguages = ["en"] or ["en", "zh-cn"]
  3. For each page node:
     a. Read Markdown content (Content for en, TranslatedContent for zh-cn)
     b. Markdig convert Markdown → HTML
     c. Wrap in HTML template (header + sidebar + main-content + footer)
     d. Write to {lang}/pages/{number}-{slug}.html
  4. Generate lang/index.html (first page, or redirect to first page)
```

### HTML Template

```html
<!DOCTYPE html>
<html lang="{lang}">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{page.title} - {siteTitle}</title>
    <link rel="stylesheet" href="../assets/css/style.css">
</head>
<body>
    <nav class="sidebar">
        <!-- Generated from sidebar.json client-side or server-side -->
    </nav>
    <main class="content">
        {markdown-to-html output}
    </main>
    <script src="../assets/js/sidebar.js"></script>
</body>
</html>
```

### Root index.html (Language Selection)

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <script>
        const lang = navigator.language.toLowerCase();
        if (lang.startsWith('zh')) {
            window.location.href = '/zh-cn/';
        } else {
            window.location.href = '/en/';
        }
    </script>
</head>
<body>
    <p>Redirecting... <a href="/en/">English</a> | <a href="/zh-cn/">中文</a></p>
</body>
</html>
```

## Constraints

- MUST NOT 依赖外部 CDN 或前端框架
- 侧边栏 MUST 与 DocumentNode 层级一一对应
- CSS/JS 总计 MUST ≤ 50KB
- Markdown 为主输出格式（澄清 Q4），静态站点为附加格式
- 单语言模式（翻译关闭）MUST 仅生成 `en/` 目录 + `index.html` 直接跳转（澄清 Q3）

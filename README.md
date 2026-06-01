# DeepWikiFetcher

DeepWikiFetcher 是一个 .NET 10 文档抓取工具。输入 GitHub 仓库 URL 后，工具会转换到 DeepWiki 文档站，抓取页面目录和正文，并输出 Markdown 文档集或离线静态站点。项目同时提供 CLI 入口和 .NET MAUI Windows 桌面端。

## 功能

| 功能 | 说明 |
|------|------|
| DeepWiki 抓取 | 从 GitHub 仓库 URL 映射到 DeepWiki URL，解析侧边栏并抓取页面。 |
| Markdown 输出 | 按层级编号生成 Markdown 文件，包含 YAML frontmatter。 |
| 静态站点输出 | 生成离线 HTML、`sidebar.json`、`config.js`、CSS 和 JS。 |
| 中英双语 | 通过 OpenAI 兼容 `/v1/chat/completions` API 生成中文内容。 |
| SQLite 缓存 | 缓存页面 HTML、翻译结果和爬取元数据。 |
| MAUI 桌面端 | 提供设置、抓取和历史三页图形界面。 |

## 快速开始

### CLI

```powershell
dotnet build DeepWikiFetcher.slnx
dotnet run --project DeepWikiFetcher.Host -- --url https://github.com/owner/repo --output ./Output
```

启用静态站点输出：

```powershell
dotnet run --project DeepWikiFetcher.Host -- --url https://github.com/owner/repo --output ./Output --format StaticSite
```

启用翻译：

```powershell
Copy-Item DeepWikiFetcher.Host/appsettings.template.json DeepWikiFetcher.Host/appsettings.json
dotnet run --project DeepWikiFetcher.Host -- --url https://github.com/owner/repo --output ./Output --translate
```

### MAUI Desktop

```powershell
dotnet build DeepWikiFetcher.Desktop/DeepWikiFetcher.Desktop.csproj -f net10.0-windows10.0.19041.0
dotnet run --project DeepWikiFetcher.Desktop -f net10.0-windows10.0.19041.0
```

## 项目结构

```text
DeepWikiFetcher.Host/             CLI 与 Minimal API 入口。
DeepWikiFetcher.Desktop/          .NET MAUI 桌面端。
DeepWikiFetcher.Services/         URL 转换、解析、清洗、翻译和输出生成。
DeepWikiFetcher.Infrastructure/   SQLite、HTTP、Polly、翻译 API 和资源下载。
DeepWikiFetcher.Shared/           模型、枚举和配置选项。
DeepWikiFetcher.Tests/            xUnit 单元测试。
```

## 常用验证

```powershell
dotnet test DeepWikiFetcher.Tests/DeepWikiFetcher.Tests.csproj
dotnet build DeepWikiFetcher.Host/DeepWikiFetcher.Host.csproj
dotnet build DeepWikiFetcher.Desktop/DeepWikiFetcher.Desktop.csproj -f net10.0-windows10.0.19041.0
```
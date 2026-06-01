# Quickstart: 中英双语翻译、静态站点输出与 MAUI 桌面端

**Date**: 2026-05-31
**Related**: [plan.md](./plan.md) | [spec.md](./spec.md)

## Prerequisites

- .NET 10 SDK
- Windows 10 19041+ (MAUI 桌面端)
- OpenAI 兼容 API 端点（可选，仅翻译功能需要）

## Quick Start: CLI

### 1. 配置翻译（可选）

```sh
cp DeepWikiFetcher.Host/appsettings.template.json DeepWikiFetcher.Host/appsettings.json
```

编辑 `appsettings.json`，按需设置：

```json
{
  "Translation": {
    "Enabled": true,
    "BaseUrl": "https://api.openai.com",
    "ApiKey": "sk-...",
    "Model": "gpt-4o",
    "MaxConcurrency": 1,
    "BatchSize": 10,
    "RequestDelayMs": 1000
  }
}
```

### 2. 执行爬取

```sh
# Markdown 输出 + 翻译
dotnet run --project DeepWikiFetcher.Host -- \
  --url https://github.com/owner/repo \
  --output ./Output \
  --translate

# 静态站点输出 + 翻译
dotnet run --project DeepWikiFetcher.Host -- \
  --url https://github.com/owner/repo \
  --output ./Output \
  --format StaticSite \
  --translate

# 仅英文 Markdown（翻译关闭）
dotnet run --project DeepWikiFetcher.Host -- \
  --url https://github.com/owner/repo \
  --output ./Output
```

### 3. 查看输出

```text
Output/{owner}/{repo}/
├── index.html          # 浏览器打开 → 语言选择
├── zh-cn/              # 中文内容（翻译启用时）
│   ├── pages/
│   │   ├── 1-installation.md
│   │   └── ...
│   ├── _index.json
│   └── _sidebar.md
├── en/                 # 英文内容（始终存在）
│   └── ...
└── assets/images/
```

## Quick Start: MAUI Desktop

### 1. 启动应用

```sh
dotnet build DeepWikiFetcher.slnx
dotnet run --project DeepWikiFetcher.Desktop
```

### 2. 使用流程

1. **设置页**：填写 GitHub URL、选择输出目录、选择格式（Markdown/StaticSite）、配置翻译（API Key 加密存储）
2. **抓取页**：点击"开始抓取"，观察实时进度条和日志流
3. **历史页**：查看历次爬取记录，点击打开输出目录

## Development Setup

### Build

```sh
dotnet build DeepWikiFetcher.slnx
```

### Project Dependency Graph

```
Host ──→ Services ──→ Infrastructure ──→ Shared
Desktop ──→ Services ──→ Infrastructure ──→ Shared
```

### Key Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `Shared/Models/CrawlProgress.cs` | CREATE | 进度报告 DTO |
| `Shared/Models/TranslationCacheEntry.cs` | CREATE | 翻译缓存记录 |
| `Shared/Models/AssetInfo.cs` | CREATE | 图片资源信息 |
| `Shared/Models/SidebarEntry.cs` | CREATE | 侧边栏条目 |
| `Shared/Models/StaticSiteConfig.cs` | CREATE | 静态站点配置 |
| `Shared/Models/DocumentNode.cs` | MODIFY | 新增 TranslatedContent |
| `Shared/Models/CleanResult.cs` | MODIFY | 新增 AssetInfos |
| `Shared/Options/TranslationOptions.cs` | MODIFY | 新增 MaxConcurrency、BatchSize 等 |
| `Infrastructure/Interfaces/ITranslationApiClient.cs` | CREATE | 翻译 API 接口 |
| `Infrastructure/Interfaces/IAssetDownloader.cs` | CREATE | 图片下载器接口 |
| `Infrastructure/Services/TranslationApiClient.cs` | CREATE | 翻译 API 实现 |
| `Infrastructure/Services/AssetDownloader.cs` | CREATE | 图片下载实现 |
| `Services/Interfaces/ITranslationService.cs` | CREATE | 翻译服务接口 |
| `Services/Services/TranslationService.cs` | CREATE | 翻译服务实现 |
| `Services/Services/StaticSiteGenerator.cs` | CREATE | 静态站点生成器 |
| `Host/Program.cs` | MODIFY | 新增 CLI 参数 |
| `Host/appsettings.template.json` | MODIFY | 新增 Translation 节 |
| `Desktop/ViewModels/*.cs` | CREATE | 3 个 ViewModel |
| `Desktop/Pages/*.xaml/.cs` | MODIFY | 完整 UI 实现 |
| `Desktop/MauiProgram.cs` | MODIFY | 注册 ViewModels + Services |
| `docs/design/database.md` | CREATE | 完整 DDL |
| `docs/design/data-model.md` | CREATE | 所有 DTO |
| `README.md` | MODIFY | 项目说明、快速开始 |

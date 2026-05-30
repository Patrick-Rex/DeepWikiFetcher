<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan.

**Architecture document**: All spec-kit agents (speckit.plan, speckit.specify,
speckit.tasks) MUST read `docs/design/architecture.md` before producing any
output. This document is the authoritative source for layered architecture
(Host → Services → Infrastructure → Data), service boundaries, dual-mode
content fetching, concurrency model, and component contracts.
<!-- SPECKIT END -->

# DeepWikiFetcher

## TL;DR
C# .NET 10 爬虫：输入 GitHub 仓库 URL → 下载 DeepWiki 文档集为 Markdown。
Host 层只做 DI + 启动，核心逻辑在 Services/ 和 Infrastructure/。

## Build & Run
```sh
dotnet build
dotnet run --project DeepWikiFetcher.Host
```

## Reference
权威规则以 `.specify/memory/constitution.md` 为准。Code conventions、技术栈约束、弹性策略均见 Constitution。本文件仅补充 Constitution 未覆盖的文档治理规则。

---

## Documentation Governance

### Index
`docs/README.md` 是 docs 的唯一索引文件。任何 docs 内文件的增删改必须同步更新 `docs/README.md` 中的索引条目。索引格式：

```markdown
## 文件索引

| 文件 | 描述 |
|------|------|
| `design/architecture.md` | 系统架构：分层设计、服务边界、数据流 |
| `design/tech-stack.md` | 技术选型及版本约束 |
```

### Folder Structure

`docs/` 目录结构如下，新增文件 MUST 按类型归入对应文件夹：

```text
docs/
├── README.md              唯一索引，增删文件必须同步更新
├── design/                 架构设计、技术选型、组件交互、数据流等
├── spec/                   编写规范、命名规范、代码审查清单等
└── guides/                 操作指南、环境搭建、故障排查等
```

**分类规则**：

| 文档类型 | 目标文件夹 | 示例 |
|----------|-----------|------|
| 怎么写（规范/标准） | `spec/` | Markdown 规范、命名规范、Commit 规范 |
| 怎么设计（架构/技术） | `design/` | 架构设计、技术选型、模块设计、接口契约 |
| 怎么用（指南/操作） | `guides/` | 环境搭建、本地调试、部署指南 |

### AI-Optimized Markdown Spec

`docs/` 下所有 `.md` 文件遵循 `docs/spec/ai-markdown.md` 中定义的 AI 优化 Markdown 规范。

### C# Coding Standard

所有 `.cs` 文件 MUST 遵循 `docs/spec/csharp-coding-standard.md` 中定义的 C# 代码编写规范。该规范是 C# 代码权威来源，聚合了 Constitution、tech-stack 及散落约定。

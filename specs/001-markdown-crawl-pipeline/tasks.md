# Tasks: DeepWikiFetcher 项目骨架与 Markdown 爬取流水线

**Input**: Design documents from `specs/001-markdown-crawl-pipeline/`
**Prerequisites**: spec.md (required), architecture.md (required)

> **⚠️ READ FIRST**: All task assignments respect `docs/design/architecture.md` layered
> architecture (Host → Services → Infrastructure → Shared). Phase 2 (Shared models +
> interfaces) blocks all user story work.

**Tests**: Not requested in spec — test tasks omitted.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label ([US1], [US2], [US3])
- Exact file paths in every description

---

## Phase 1: Setup (项目骨架)

**Purpose**: Create 5-project solution structure with correct dependency graph

- [x] T001 Create solution file `DeepWikiFetcher.slnx` with all 5 projects referenced
- [x] T002 [P] Create `DeepWikiFetcher.Shared/DeepWikiFetcher.Shared.csproj` — net10.0, Nullable enable, file-scoped namespaces, no external package references
- [x] T003 [P] Create `DeepWikiFetcher.Infrastructure/DeepWikiFetcher.Infrastructure.csproj` — net10.0, Nullable enable, project reference to Shared, add NuGet packages (Microsoft.Data.Sqlite, Polly, Polly.Extensions, Microsoft.Extensions.Http)
- [x] T004 [P] Create `DeepWikiFetcher.Services/DeepWikiFetcher.Services.csproj` — net10.0, Nullable enable, project reference to Infrastructure, add NuGet packages (AngleSharp, Markdig)
- [x] T005 [P] Create `DeepWikiFetcher.Host/DeepWikiFetcher.Host.csproj` — net10.0, Nullable enable, project reference to Services, add NuGet packages (Microsoft.Extensions.Hosting, Microsoft.Extensions.Configuration.Json, System.CommandLine)
- [x] T006 [P] Create `DeepWikiFetcher.Desktop/DeepWikiFetcher.Desktop.csproj` — net10.0-ios/net10.0-maccatalyst/net10.0-android/net10.0-windows10.0.19041.0 (MAUI targets), Nullable enable, project reference to Services
- [x] T007 Create `.gitignore` — exclude `appsettings.json`, `appsettings.Development.json`, `*.db`, `*.db-shm`, `*.db-wal`, `Output/`, `bin/`, `obj/`
- [x] T008 Create `DeepWikiFetcher.Host/appsettings.template.json` — all config sections: Crawler (RateLimitPerMinute=30, MaxRetryCount=3, CircuitBreakerThreshold=5, CircuitBreakerDurationSeconds=30, MaxConcurrency=3, ChannelCapacity=100, CacheExpirationHours=24, OutputFormat="Markdown"), Playwright (Enabled=false, BrowserPath="", Headless=true), Translation (Enabled=false, BaseUrl="", ApiKey="", Model=""), Logging (LogLevel:Default="Information")

**Checkpoint**: `dotnet build` succeeds — all 5 projects compile with zero warnings.

---

## Phase 2: Foundational (Shared 层模型 + 接口定义)

**Purpose**: All models, enums, config classes, and interfaces that EVERY user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Shared Models & Config

- [x] T009 [P] Define `OutputFormat` enum (`Markdown`, `StaticSite`) in `DeepWikiFetcher.Shared/Enums/OutputFormat.cs`
- [x] T010 [P] Define `DocumentNode` model (`Title`, `TranslatedTitle?`, `Url`, `Depth`, `Number`, `Children`) with XML doc comments in `DeepWikiFetcher.Shared/Models/DocumentNode.cs`
- [x] T011 [P] Define `CrawlOptions` model (`GitHubUrl`, `OutputRoot`, `OutputFormat`, `TranslationEnabled`) with XML doc comments in `DeepWikiFetcher.Shared/Models/CrawlOptions.cs`
- [x] T012 [P] Define `CrawlResult` model (`RepoKey`, `TotalPages`, `SuccessCount`, `FailCount`, `Duration`, `OutputPath`) with XML doc comments in `DeepWikiFetcher.Shared/Models/CrawlResult.cs`
- [x] T013 [P] Define `CleanResult` model (`CleanHtml`, `ImageUrls`) with XML doc comments in `DeepWikiFetcher.Shared/Models/CleanResult.cs`
- [x] T014 [P] Define `PageCacheEntry` record (`UrlHash`, `Url`, `Content`, `CachedAt`, `ExpiresAt`) in `DeepWikiFetcher.Shared/Models/PageCacheEntry.cs`
- [x] T015 [P] Define `CrawlMetadata` record (`RepoKey`, `StartedAt`, `CompletedAt?`, `Status`, `TotalPages`, `SuccessPages`, `FailedPages`) in `DeepWikiFetcher.Shared/Models/CrawlMetadata.cs`
- [x] T016 [P] Define `CrawlerOptions` config class (RateLimitPerMinute, MaxRetryCount, CircuitBreakerThreshold, CircuitBreakerDurationSeconds, MaxConcurrency, ChannelCapacity, CacheExpirationHours, OutputFormat) in `DeepWikiFetcher.Shared/Options/CrawlerOptions.cs`
- [x] T017 [P] Define `PlaywrightOptions` config class (Enabled, BrowserPath, Headless) in `DeepWikiFetcher.Shared/Options/PlaywrightOptions.cs`
- [x] T018 [P] Define `TranslationOptions` config class (Enabled, BaseUrl, ApiKey, Model) — placeholder for future use in `DeepWikiFetcher.Shared/Options/TranslationOptions.cs`

### Service Interfaces

- [x] T019 [P] Define `IUrlTransformer` interface (`string Transform(string githubUrl)`) with XML doc in `DeepWikiFetcher.Services/Interfaces/IUrlTransformer.cs`
- [x] T020 [P] Define `ISidebarParser` interface (`Task<DocumentNode> ParseAsync(string deepWikiHomeUrl, CancellationToken ct)`) with XML doc in `DeepWikiFetcher.Services/Interfaces/ISidebarParser.cs`
- [x] T021 [P] Define `IPageFetcher` interface (`Task<string> FetchAsync(string pageUrl, CancellationToken ct)`) with XML doc in `DeepWikiFetcher.Services/Interfaces/IPageFetcher.cs`
- [x] T022 [P] Define `IHtmlCleaner` interface (`Task<CleanResult> CleanAsync(string rawHtml, string baseUrl, CancellationToken ct)`) with XML doc in `DeepWikiFetcher.Services/Interfaces/IHtmlCleaner.cs`
- [x] T023 [P] Define `IOutputGenerator` interface (`Task GenerateAsync(DocumentNode root, string outputDir, CancellationToken ct)`) with XML doc in `DeepWikiFetcher.Services/Interfaces/IOutputGenerator.cs`
- [x] T024 [P] Define `ICrawlOrchestrator` interface (`Task<CrawlResult> StartAsync(CrawlOptions options, CancellationToken ct)`) with XML doc in `DeepWikiFetcher.Services/Interfaces/ICrawlOrchestrator.cs`

### Infrastructure Interfaces

- [x] T025 [P] Define `ICacheManager` interface (methods: `Task<string?> GetPageAsync(string url)`, `Task SetPageAsync(string url, string content)`, `Task SaveMetadataAsync(CrawlMetadata metadata)`, `Task<CrawlMetadata?> GetMetadataAsync(string repoKey)`) with XML doc in `DeepWikiFetcher.Infrastructure/Interfaces/ICacheManager.cs`
- [x] T026 [P] Define `IPollyPipeline` interface (method: `IAsyncPolicy<HttpResponseMessage> CreatePipeline(CrawlerOptions options)`) with XML doc in `DeepWikiFetcher.Infrastructure/Interfaces/IPollyPipeline.cs`

### DI Registration Stub

- [x] T027 Register all interface-to-empty-implementation mappings in `DeepWikiFetcher.Host/Program.cs` — DI container wires up interfaces from Phase 2 with `AddSingleton`/`AddScoped` stubs, `IOptions<T>` binds `appsettings.json` sections, startup prints "DeepWikiFetcher ready"

**Checkpoint**: Foundation ready — all interfaces & models compile, DI container resolves all services (stubs). User story implementation can now begin.

---

## Phase 3: User Story 1 - CLI 一键爬取 GitHub 仓库文档 (Priority: P1) 🎯 MVP

**Goal**: End-to-end pipeline: GitHub URL → DeepWiki URL → sidebar parse → page download → HTML clean → Markdown output

**Independent Test**: `dotnet run -- --url https://github.com/owner/repo --output ./output` produces structured Markdown with `_metadata.json` and `_index.json`

### Implementation for User Story 1

- [x] T028 [US1] Implement `UrlTransformer` — GitHub URL validation + DeepWiki URL mapping in `DeepWikiFetcher.Services/Services/UrlTransformer.cs`
- [x] T029 [US1] Implement `SidebarParser` — AngleSharp DOM parse, extract nav tree, recursive `DocumentNode` build with depth numbering in `DeepWikiFetcher.Services/Services/SidebarParser.cs`
- [x] T030 [US1] Implement `PageFetcher` — `HttpClient` GET with `IAsyncPolicy<HttpResponseMessage>` Polly pipeline injection, Playwright fallback stub (throw `NotSupportedException` when enabled but not configured) in `DeepWikiFetcher.Services/Services/PageFetcher.cs`
- [x] T031 [US1] Implement `HtmlCleaner` — AngleSharp: remove `<nav>`, `<footer>`, keep `<article>`/`.content`, fix relative→absolute links, extract `<img>` src list in `DeepWikiFetcher.Services/Services/HtmlCleaner.cs`
- [x] T032 [US1] Implement `MarkdownWriter` (implements `IOutputGenerator`) — Markdig `HtmlToMarkdown`, slug generation (`{number}-{slug}.md`), YAML frontmatter (`title`, `url`, `depth`), write files to `{output}/{owner}/{repo}/` in `DeepWikiFetcher.Services/Services/MarkdownWriter.cs`
- [x] T033 [US1] Implement `CrawlOrchestrator` — `Channel<DocumentNode>` bounded producer-consumer (capacity from `CrawlerOptions.ChannelCapacity`), `SemaphoreSlim` concurrency control, coordinate UrlTransformer→SidebarParser→[PageFetcher→HtmlCleaner per page]→MarkdownWriter in `DeepWikiFetcher.Services/Services/CrawlOrchestrator.cs`
- [x] T034 [US1] Implement JSON output — `_metadata.json` (serialize `CrawlResult`) and `_index.json` (serialize root `DocumentNode` tree) using `System.Text.Json` with PascalCase in `DeepWikiFetcher.Services/Services/OutputSerializer.cs`
- [x] T035 [US1] Wire CLI — parse `--url` and `--output` args with System.CommandLine, build `CrawlOptions`, invoke `ICrawlOrchestrator.StartAsync`, print CrawlResult summary to console in `DeepWikiFetcher.Host/Program.cs`
- [x] T036 [US1] Add structured logging — `ILogger<T>` Info/Warning/Error at key pipeline milestones (phase start/complete, page success/fail, cache hit/miss) across all Services implementations

**Checkpoint**: CLI pipeline fully functional — `dotnet run -- --url <valid-github-url> --output ./output` produces complete Markdown document set

---

## Phase 4: User Story 2 - 弹性容错保障爬取稳定性 (Priority: P2)

**Goal**: Polly resilience pipeline + SQLite cache + incremental crawl — network failures don't interrupt crawl

**Independent Test**: Simulate 503 responses, verify automatic retry; clear cache and re-run, verify cache hits skip network

### Implementation for User Story 2

- [x] T037 [US2] Implement `PollyPipeline` — build `IAsyncPolicy<HttpResponseMessage>` combining RateLimit (30/min via `RateLimitAsyncPolicy`), Retry (exponential backoff $2^n \times 1s$, max 3 retries, handle 429/5xx), CircuitBreaker (5 consecutive failures → 30s open) in `DeepWikiFetcher.Infrastructure/Services/PollyPipeline.cs`
- [x] T038 [US2] Implement `CacheManager` — SQLite DB init (create `page_cache` and `crawl_metadata` tables if not exist), `GetPageAsync` (SHA256 lookup with 24h expiry check), `SetPageAsync` (insert/update), `SaveMetadataAsync`/`GetMetadataAsync` in `DeepWikiFetcher.Infrastructure/Services/CacheManager.cs`
- [x] T039 [US2] Integrate Polly pipeline into `PageFetcher` — replace inline HttpClient with `IAsyncPolicy<HttpResponseMessage>` from `IPollyPipeline`, all config parameters read from `IOptions<CrawlerOptions>` in `DeepWikiFetcher.Services/Services/PageFetcher.cs`
- [x] T040 [US2] Integrate SQLite cache into `PageFetcher` — `FetchAsync` checks `ICacheManager.GetPageAsync` first, cache hit returns immediately (skip HTTP), cache miss downloads + calls `SetPageAsync` in `DeepWikiFetcher.Services/Services/PageFetcher.cs`
- [x] T041 [US2] Integrate cache into `CrawlOrchestrator` — save `CrawlMetadata` via `ICacheManager.SaveMetadataAsync` at crawl start and end in `DeepWikiFetcher.Services/Services/CrawlOrchestrator.cs`
- [x] T042 [US2] Update `appsettings.template.json` — add RateLimit section (RequestsPerMinute, MinIntervalMs), Retry section (MaxRetryCount, BaseDelaySeconds), CircuitBreaker section (FailureThreshold, DurationSeconds) with comments in `DeepWikiFetcher.Host/appsettings.template.json`
- [x] T043 [US2] Update `CrawlerOptions` — add nested config classes or flatten: `RateLimitRequestsPerMinute`, `MinIntervalMs`, `BaseDelaySeconds` in `DeepWikiFetcher.Shared/Options/CrawlerOptions.cs`

**Checkpoint**: Pipeline survives network instability — retry and circuit breaker functional, cache accelerates repeat crawls

---

## Phase 5: User Story 3 - MAUI 桌面端空壳就绪 (Priority: P3)

**Goal**: MAUI Shell app with three tab pages (设置/抓取/历史), each showing centered placeholder text

**Independent Test**: Launch MAUI app, verify three tabs render and switch correctly

### Implementation for User Story 3

- [x] T044 [US3] Configure `DeepWikiFetcher.Desktop.csproj` — verify MAUI SDK targets (net10.0-windows10.0.19041.0 minimum for desktop), add `Microsoft.Maui.Controls` package reference in `DeepWikiFetcher.Desktop/DeepWikiFetcher.Desktop.csproj`
- [x] T045 [US3] Create `App.xaml` + `App.xaml.cs` — MAUI `Application` subclass, set `MainPage = new AppShell()` in `DeepWikiFetcher.Desktop/App.xaml` and `DeepWikiFetcher.Desktop/App.xaml.cs`
- [x] T046 [US3] Create `AppShell.xaml` + `AppShell.xaml.cs` — Shell with three `Tab` items routing to SettingsPage, CrawlPage, HistoryPage in `DeepWikiFetcher.Desktop/AppShell.xaml` and `DeepWikiFetcher.Desktop/AppShell.xaml.cs`
- [x] T047 [P] [US3] Create `SettingsPage.xaml` + `SettingsPage.xaml.cs` — centered `Label` "配置抓取参数 — 即将推出" in `DeepWikiFetcher.Desktop/Pages/SettingsPage.xaml` and `DeepWikiFetcher.Desktop/Pages/SettingsPage.xaml.cs`
- [x] T048 [P] [US3] Create `CrawlPage.xaml` + `CrawlPage.xaml.cs` — centered `Label` "抓取控制面板 — 即将推出" in `DeepWikiFetcher.Desktop/Pages/CrawlPage.xaml` and `DeepWikiFetcher.Desktop/Pages/CrawlPage.xaml.cs`
- [x] T049 [P] [US3] Create `HistoryPage.xaml` + `HistoryPage.xaml.cs` — centered `Label` "历史记录 — 即将推出" in `DeepWikiFetcher.Desktop/Pages/HistoryPage.xaml` and `DeepWikiFetcher.Desktop/Pages/HistoryPage.xaml.cs`
- [x] T050 [US3] Create `MauiProgram.cs` — `CreateMauiApp()` builder, register MAUI services, call `UseMauiApp<App>()` in `DeepWikiFetcher.Desktop/MauiProgram.cs`

**Checkpoint**: MAUI app launches with three tabs, each displaying correct placeholder text

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Code quality, standards compliance, build verification

- [x] T051 [P] Audit all public classes/interfaces — every public member has XML doc comment (`<summary>`, `<param>`, `<returns>`, `<exception>`) per FR-027 across all 5 projects
- [x] T052 [P] Verify `.gitignore` covers all exclusions per FR-026 — `appsettings.json`, `appsettings.Development.json`, `*.db`, `*.db-shm`, `*.db-wal`, `Output/` at repository root
- [x] T053 Verify `dotnet build` succeeds with zero warnings on all 5 projects — fix any remaining warnings
- [x] T054 Verify dependency direction — no reverse references (Shared must not reference any other project, Infrastructure must not reference Services/Host/Desktop, Services must not reference Host/Desktop) using project reference audit
- [x] T055 Run end-to-end smoke test — `dotnet run --project DeepWikiFetcher.Host -- --url https://github.com/dotnet/docs --output ./test-output`, verify output files exist and are valid

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ──────────────────────────────────────────────┐
    │                                                        │
    ▼                                                        │
Phase 2: Foundational (Models + Interfaces + DI Stubs)       │
    │                  ⚠️ BLOCKS ALL USER STORIES            │
    ├──────────────┬──────────────────┬──────────────────────┤
    ▼              ▼                  ▼                      │
Phase 3: US1     Phase 4: US2       Phase 5: US3            │
    │              │                  │                      │
    │              │ (US2 extends     │                      │
    │              │  PageFetcher     │                      │
    │              │  from US1)       │                      │
    │              ▼                  │                      │
    │         [requires T030          │                      │
    │          from US1]              │                      │
    │                                 │                      │
    └──────────────┴──────────────────┴──────────────────────┘
                            │
                            ▼
                   Phase 6: Polish
```

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 only — can start immediately after Foundational. No other story dependencies.
- **US2 (P2)**: Depends on Phase 2 + T030 (PageFetcher from US1) — Polly pipeline and cache integrate into existing PageFetcher. T037-T038 (Infrastructure implementations) can start in parallel with US1. T039-T040 require T030 complete.
- **US3 (P3)**: Depends on Phase 2 only (references Services project but uses only interfaces/DI, not implementations). Can start in parallel with US1 and US2.

### Within Each User Story

- US1: T028 (UrlTransformer) → T029 (SidebarParser) → T030 (PageFetcher) → T031 (HtmlCleaner) → T032+T034 (output) → T033 (orchestrator) → T035 (CLI wire) → T036 (logging)
- US2: T037+T043 (Polly+CrawlerOptions) | T038 (CacheManager) → T039 (Polly integration) + T040 (Cache integration) → T041 (orchestrator metadata) → T042 (template update)
- US3: T044 (csproj) → T045 (App) → T046 (Shell) + T047+T048+T049 (pages) → T050 (MauiProgram)

### Parallel Opportunities

```
Phase 2 Parallelism:
  T009-T018 (Shared models): ALL 10 can run in parallel
  T019-T024 (Service interfaces): ALL 6 can run in parallel
  T025-T026 (Infrastructure interfaces): Both can run in parallel
  After models complete: T027 (DI stub)

Phase 3 (US1) Parallelism:
  T028 → T029 → T030 → T031 → [T032 ∥ T034] → T033 → T035 → T036

Phase 4 (US2) Parallelism:
  [T037+T043 ∥ T038] → [T039 ∥ T040] → T041 → T042

Phase 5 (US3) Parallelism:
  T044 → T045 → T046 + [T047 ∥ T048 ∥ T049] → T050

Phase 6 Parallelism:
  T051 ∥ T052 (both read-only audits)
  Then: T053 → T054 → T055 (sequential verification)
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup → `dotnet build` passes
2. Complete Phase 2: Foundational → all models + interfaces compile
3. Complete Phase 3: US1 → CLI pipeline works end-to-end
4. **STOP and VALIDATE**: `dotnet run -- --url <real-repo> --output ./test-output` produces real Markdown
5. Deploy/demo if ready — this is a working product

### Incremental Delivery

| Stage | Phases | Deliverable |
|-------|--------|-------------|
| MVP | 1 + 2 + 3 | CLI crawler: URL → Markdown files |
| v1.1 | + Phase 4 | Resilient crawler: retry + cache + circuit breaker |
| v1.2 | + Phase 5 | MAUI desktop shell (placeholder UI) |
| Release | + Phase 6 | Production-ready: documented, linted, verified |

### Task Count Summary

| Phase | Tasks | Story |
|-------|-------|-------|
| Phase 1: Setup | 8 (T001-T008) | — |
| Phase 2: Foundational | 19 (T009-T027) | — |
| Phase 3: US1 | 9 (T028-T036) | US1 |
| Phase 4: US2 | 7 (T037-T043) | US2 |
| Phase 5: US3 | 7 (T044-T050) | US3 |
| Phase 6: Polish | 5 (T051-T055) | — |
| **Total** | **55** | **3 stories** |

---
title: "SQLite 数据库设计"
updated: "2026-06-01"
---

# SQLite 数据库设计

## 概览

DeepWikiFetcher 使用 SQLite 作为本地缓存数据库。数据库文件名为 `cache.db`，默认位于运行输出根目录或当前工作目录。数据库用于页面缓存、翻译缓存和爬取元数据记录。

## 表结构

### page_cache

`page_cache` 表缓存 DeepWiki 页面 HTML。缓存键为 URL 的 SHA256 哈希。

```sql
CREATE TABLE IF NOT EXISTS page_cache (
    url_hash TEXT PRIMARY KEY,
    url TEXT NOT NULL,
    content TEXT NOT NULL,
    cached_at TEXT NOT NULL,
    expires_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_page_cache_expires
ON page_cache(expires_at);
```

### translation_cache

`translation_cache` 表缓存整页 Markdown 翻译结果。缓存键为原文正文的 SHA256 哈希，并通过 `model` 字段区分不同模型生成的结果。

```sql
CREATE TABLE IF NOT EXISTS translation_cache (
    source_hash TEXT PRIMARY KEY,
    page_url TEXT NOT NULL,
    source_text TEXT NOT NULL,
    translated_text TEXT NOT NULL,
    model TEXT NOT NULL DEFAULT '',
    cached_at TEXT NOT NULL,
    expires_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_translation_cache_page_url
ON translation_cache(page_url);

CREATE INDEX IF NOT EXISTS idx_translation_cache_model
ON translation_cache(model);

CREATE INDEX IF NOT EXISTS idx_translation_cache_expires
ON translation_cache(expires_at);
```

### crawl_metadata

`crawl_metadata` 表记录仓库级爬取历史。MAUI 历史页读取该表展示历史记录。

```sql
CREATE TABLE IF NOT EXISTS crawl_metadata (
    repo_key TEXT PRIMARY KEY,
    started_at TEXT NOT NULL,
    completed_at TEXT,
    status TEXT NOT NULL DEFAULT 'Running',
    total_pages INTEGER NOT NULL DEFAULT 0,
    success_pages INTEGER NOT NULL DEFAULT 0,
    failed_pages INTEGER NOT NULL DEFAULT 0
);
```

## 迁移策略

`CacheManager.InitializeDatabase` 在应用启动时执行 `CREATE TABLE IF NOT EXISTS` 和 `CREATE INDEX IF NOT EXISTS`。迁移过程是幂等的，重复启动不会破坏既有数据。

## 缓存规则

| 缓存类型 | 键 | 过期字段 | 默认过期 |
|----------|----|----------|----------|
| 页面缓存 | `SHA256(url)` | `expires_at` | 24 小时。 |
| 翻译缓存 | `SHA256(source_text)` | `expires_at` | 30 天。 |

## 查询约束

- 页面缓存查询 MUST 同时检查 `url_hash` 和 `expires_at > now`。
- 翻译缓存查询 MUST 同时检查 `source_hash`、`model` 和 `expires_at > now`。
- 缓存写入 MUST 使用 `INSERT OR REPLACE` 保持幂等。
- 失败页面和失败翻译 MUST 记录日志，MUST NOT 删除已有缓存。
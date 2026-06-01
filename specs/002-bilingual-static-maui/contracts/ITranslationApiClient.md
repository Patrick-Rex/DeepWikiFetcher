# Contract: ITranslationApiClient

**Layer**: Infrastructure
**Namespace**: `DeepWikiFetcher.Infrastructure.Interfaces`

## Interface Definition

```csharp
namespace DeepWikiFetcher.Infrastructure.Interfaces;

/// <summary>
/// OpenAI 兼容翻译 API 客户端接口。
/// 封装 /v1/chat/completions 端点，支持任意兼容服务。
/// </summary>
public interface ITranslationApiClient
{
    /// <summary>
    /// 发送翻译请求。
    /// </summary>
    /// <param name="sourceText">待翻译的 Markdown 原文（代码块已占位符保护）</param>
    /// <param name="model">模型名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>翻译后的 Markdown 文本（结构不变，仅翻译自然语言）；失败返回原文</returns>
    Task<string> TranslateAsync(string sourceText, string model, CancellationToken cancellationToken = default);
}
```

## Endpoint Contract

### Request

```http
POST {BaseUrl}/v1/chat/completions
Content-Type: application/json
Authorization: Bearer {ApiKey}
```

```json
{
  "model": "{Model}",
  "messages": [
    {
      "role": "system",
      "content": "You are a technical documentation translator. Translate English to Simplified Chinese. Rules: 1) DO NOT translate code blocks, inline code, URLs, or HTML tags. 2) Preserve ALL Markdown formatting exactly - headings, lists, links, tables, code fences. 3) Only translate natural language text. 4) Return the translated Markdown directly, no explanations."
    },
    {
      "role": "user",
      "content": "{Markdown with placeholders for code/URLs}"
    }
  ],
  "temperature": 0.1,
  "max_tokens": 16384
}
```

### Response

```json
{
  "id": "chatcmpl-xxx",
  "object": "chat.completion",
  "created": 1234567890,
  "model": "gpt-4o",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "{translated Markdown}"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 500,
    "completion_tokens": 600,
    "total_tokens": 1100
  }
}
```

## Behavior Contract

| Scenario | Behavior |
|----------|----------|
| HTTP 2xx + valid JSON | 返回 `choices[0].message.content` |
| HTTP 非 2xx | 触发 Polly 重试（最多 3 次，指数退避） |
| 重试耗尽 | 返回原文 `sourceText`（降级），记录 Error 日志 |
| JSON 解析失败 | 返回原文，记录 Error 日志 |
| `cancellationToken` 触发 | 抛出 `OperationCanceledException` |
| `sourceText` 为空 | 立即返回 `string.Empty`，不发请求 |

## Configuration

通过 `TranslationOptions` 注入（`IOptions<TranslationOptions>`）：

| Config Key | Description |
|------------|-------------|
| `Translation.BaseUrl` | API 基础地址 |
| `Translation.ApiKey` | API 密钥 |
| `Translation.Model` | 默认模型 |
| `Translation.BatchSize` | 翻译批大小，默认 10 页/批（由 TranslationService 使用） |
| `Translation.RequestDelayMs` | 请求间隔（毫秒） |

## Constraints

- MUST 使用 `IHttpClientFactory` 创建 HttpClient
- MUST 实现独立的 Polly 弹性管道（与爬取管道隔离）
- MUST NOT 抛出异常导致上层翻译中断（降级返回原文）
- 单次请求超时 MUST 为 30s

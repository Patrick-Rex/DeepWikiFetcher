using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DeepWikiFetcher.Infrastructure.Interfaces;
using DeepWikiFetcher.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeepWikiFetcher.Infrastructure.Services;

/// <summary>
/// OpenAI 兼容翻译 API 客户端。
/// </summary>
public sealed class TranslationApiClient : ITranslationApiClient
{
    private const double Temperature = 0.1;
    private const int MaxTokens = 16384;
    private const int MaxRetryAttempts = 3;
    private const int TimeoutSeconds = 30;
    private const string EndpointPath = "/v1/chat/completions";
    private const string SystemPrompt = "You are a technical documentation translator. Translate English to Simplified Chinese. Rules: 1) DO NOT translate code blocks, inline code, URLs, or HTML tags. 2) Preserve ALL Markdown formatting exactly - headings, lists, links, tables, code fences. 3) Only translate natural language text. 4) Return the translated Markdown directly, no explanations.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TranslationOptions _options;
    private readonly ILogger<TranslationApiClient> _logger;

    public TranslationApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationOptions> options,
        ILogger<TranslationApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> TranslateAsync(string sourceText, string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Translation API is not configured; returning source text");
            return sourceText;
        }

        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                string? result = await SendAsync(sourceText, model, cancellationToken);
                return string.IsNullOrWhiteSpace(result) ? sourceText : result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Translation request failed: attempt={Attempt}", attempt);
                if (attempt == MaxRetryAttempts)
                {
                    _logger.LogError(ex, "Translation request exhausted retries; returning source text");
                    return sourceText;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return sourceText;
    }

    private async Task<string?> SendAsync(string sourceText, string model, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

        var baseUri = new Uri(_options.BaseUrl.TrimEnd('/'));
        var requestUri = new Uri(baseUri, EndpointPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var requestBody = new ChatCompletionRequest(
            model,
            [
                new ChatMessage("system", SystemPrompt),
                new ChatMessage("user", sourceText)
            ],
            Temperature,
            MaxTokens);
        string json = JsonSerializer.Serialize(requestBody, JsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<ChatCompletionResponse>(stream, JsonOptions, cancellationToken);
        return payload?.Choices.FirstOrDefault()?.Message.Content;
    }

    private sealed record ChatCompletionRequest(
        string Model,
        List<ChatMessage> Messages,
        double Temperature,
        int MaxTokens);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatCompletionResponse(List<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
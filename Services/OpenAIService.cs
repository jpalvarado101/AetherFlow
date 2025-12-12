using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AetherFlow.Api.Config;
using Microsoft.Extensions.Options;

namespace AetherFlow.Api.Services;

public interface IOpenAIService
{
    Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<double>> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            Console.WriteLine("WARNING: OpenAI:ApiKey is not configured. Set it via appsettings or environment variables.");
        }

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.ChatModel,
            temperature = 0.2,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt   }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Chat API error {(int)response.StatusCode} {response.ReasonPhrase}: {json}");
        }

        var parsed = JsonSerializer.Deserialize<ChatResponse>(json, JsonOptions)
                     ?? throw new InvalidOperationException("Failed to deserialize chat response.");

        var content = parsed.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Chat response contained no content.");
        }

        return content;
    }

    public async Task<IReadOnlyList<double>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.EmbeddingModel,
            input = text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Embeddings API error {(int)response.StatusCode} {response.ReasonPhrase}: {json}");
        }

        var parsed = JsonSerializer.Deserialize<EmbeddingResponse>(json, JsonOptions)
                     ?? throw new InvalidOperationException("Failed to deserialize embedding response.");

        var emb = parsed.Data.FirstOrDefault()?.Embedding;
        if (emb is null || emb.Count == 0)
        {
            throw new InvalidOperationException("Embedding response was empty.");
        }

        return emb;
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } = new();
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")] public Message Message { get; set; } = new();
    }

    private sealed class Message
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")] public List<EmbeddingData> Data { get; set; } = new();
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("embedding")] public List<double> Embedding { get; set; } = new();
    }
}

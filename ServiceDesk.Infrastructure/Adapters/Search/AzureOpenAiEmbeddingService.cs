using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceDesk.Infrastructure.Adapters.Search;

public class AzureOpenAiEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureOpenAiEmbeddingService> _logger;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly bool _isConfigured;

    public AzureOpenAiEmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AzureOpenAiEmbeddingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _endpoint = configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/') ?? string.Empty;
        _apiKey = configuration["AzureOpenAI:ApiKey"] ?? string.Empty;
        _deploymentName = configuration["AzureOpenAI:EmbeddingDeploymentName"] ?? "text-embedding-3-large";

        _isConfigured = !string.IsNullOrWhiteSpace(_endpoint) &&
                        !string.IsNullOrWhiteSpace(_apiKey) &&
                        !string.IsNullOrWhiteSpace(_deploymentName);

        if (!_isConfigured)
        {
            _logger.LogWarning("[AzureOpenAI:Embedding] Missing AzureOpenAI credentials or EmbeddingDeploymentName. Vector generation will be skipped.");
        }
    }

    public bool IsConfigured => _isConfigured;

    public async Task<ReadOnlyMemory<float>?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || string.IsNullOrWhiteSpace(text))
            return null;

        var batch = await GenerateEmbeddingsBatchAsync(new[] { text }, cancellationToken);
        return batch.Count > 0 ? batch[0] : null;
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>?>> GenerateEmbeddingsBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || texts == null || texts.Count == 0)
        {
            return texts?.Select(_ => (ReadOnlyMemory<float>?)null).ToList() ?? new List<ReadOnlyMemory<float>?>();
        }

        try
        {
            string requestUrl = $"{_endpoint}/openai/deployments/{_deploymentName}/embeddings?api-version=2024-02-01";
            var requestBody = new { input = texts };
            string jsonPayload = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[AzureOpenAI:Embedding] Call failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return texts.Select(_ => (ReadOnlyMemory<float>?)null).ToList();
            }

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<EmbeddingApiResponse>(responseJson);

            if (parsed?.Data == null || parsed.Data.Count == 0)
            {
                return texts.Select(_ => (ReadOnlyMemory<float>?)null).ToList();
            }

            var results = new List<ReadOnlyMemory<float>?>();
            foreach (var item in parsed.Data)
            {
                if (item.Embedding != null && item.Embedding.Length > 0)
                {
                    results.Add(new ReadOnlyMemory<float>(item.Embedding));
                }
                else
                {
                    results.Add(null);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureOpenAI:Embedding] Exception occurred while generating embeddings.");
            return texts.Select(_ => (ReadOnlyMemory<float>?)null).ToList();
        }
    }

    private class EmbeddingApiResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData>? Data { get; set; }
    }

    private class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}

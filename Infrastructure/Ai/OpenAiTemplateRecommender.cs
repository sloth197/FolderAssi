using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FolderAssi.Application.Ai;
using FolderAssi.Domain.Ai;

namespace FolderAssi.Infrastructure.Ai;

public sealed class OpenAiTemplateRecommender : IAiTemplateRecommender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiRecommendationOptions _options;
    private readonly AiPromptBuilder _promptBuilder;
    private readonly AiRecommendationParser _parser;

    public OpenAiTemplateRecommender(
        HttpClient httpClient,
        OpenAiRecommendationOptions options,
        AiPromptBuilder promptBuilder,
        AiRecommendationParser parser)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ArgumentException("OpenAI ApiKey is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new ArgumentException("OpenAI Endpoint is required.", nameof(options));
        }

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out _))
        {
            throw new ArgumentException("OpenAI Endpoint must be a valid absolute URI.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new ArgumentException("OpenAI Model is required.", nameof(options));
        }
    }

    public async Task<TemplateRecommendationResult> RecommendAsync(
        TemplateRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildUserPrompt(request);

        using var requestMessage = BuildRequest(systemPrompt, userPrompt);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException("OpenAI request timed out or was canceled.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("OpenAI request failed due to a network error.", ex);
        }

        using var _ = response;
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI request failed with status {(int)response.StatusCode}: {rawResponse}");
        }

        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            throw new InvalidOperationException("OpenAI response body is empty.");
        }

        var aiContent = ExtractAssistantContent(rawResponse);
        return _parser.Parse(aiContent);
    }

    private HttpRequestMessage BuildRequest(string systemPrompt, string userPrompt)
    {
        var payload = new
        {
            model = _options.Model,
            temperature = 0,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = systemPrompt
                },
                new
                {
                    role = "user",
                    content = userPrompt
                }
            }
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);

        var message = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return message;
    }

    private static string ExtractAssistantContent(string rawResponse)
    {
        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            var root = document.RootElement;

            if (!root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("OpenAI response does not contain a valid choices array.");
            }

            var first = choices[0];
            if (!first.TryGetProperty("message", out var message)
                || message.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("OpenAI response does not contain choices[0].message.");
            }

            if (!message.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    "OpenAI response does not contain a string value at choices[0].message.content.");
            }

            var text = content.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("OpenAI message content is empty.");
            }

            return text;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("OpenAI response is not valid JSON.", ex);
        }
    }
}

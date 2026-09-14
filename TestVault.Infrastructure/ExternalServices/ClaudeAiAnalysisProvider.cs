using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using TestVault.Application.Interfaces;

namespace TestVault.Infrastructure.ExternalServices;

/// <summary>
/// Calls the Anthropic Claude API to analyze a failed test. Near-verbatim
/// port of the axios call in backend/routes/analysis.js:38-56 - same
/// endpoint, model, headers, request body, and the ```json fence-stripping
/// used to parse the reply.
///
/// Registered as a typed HttpClient (see DependencyInjection.cs) behind the
/// vendor-agnostic IAiAnalysisProvider interface - swapping to a different
/// provider means adding a new class here, not touching AiAnalysisService.
/// </summary>
public class ClaudeAiAnalysisProvider : IAiAnalysisProvider
{
    // Model name analysis.js:41 asks Claude for - unrelated to whichever
    // model is answering *this* migration's own conversation.
    private const string Model = "claude-sonnet-4-6";
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ClaudeAiAnalysisProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api.anthropic.com/");

        _apiKey = configuration["AI:ApiKey"]
            ?? throw new InvalidOperationException(
                "AI:ApiKey is not configured. Set it via an environment variable or User Secrets - never commit it to appsettings.json.");
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(string prompt, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Content = JsonContent.Create(new ClaudeRequest
        {
            Model = Model,
            MaxTokens = 1000,
            Messages = [new ClaudeMessage { Role = "user", Content = prompt }]
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ClaudeResponse>(ResponseJsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Claude API returned an empty response.");

        var rawText = payload.Content.FirstOrDefault()?.Text?.Trim();
        if (string.IsNullOrEmpty(rawText))
        {
            throw new InvalidOperationException("Claude API response did not contain any content.");
        }

        // Strip ```json fences exactly like analysis.js:55 does, in case
        // the model wraps its reply in a markdown code block.
        var clean = rawText.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();

        var parsed = JsonSerializer.Deserialize<ClaudeAnalysisPayload>(clean, ResponseJsonOptions)
            ?? throw new InvalidOperationException("Could not parse the AI analysis response as JSON.");

        return new AiAnalysisResult
        {
            RootCause = parsed.RootCause,
            FixSuggestion = parsed.FixSuggestion
        };
    }

    // ---- Anthropic Messages API request/response shapes ----------------

    private class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]
        public ClaudeMessage[] Messages { get; set; } = [];
    }

    private class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public List<ClaudeContentBlock> Content { get; set; } = [];
    }

    private class ClaudeContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    // The JSON the model is instructed to reply with - see
    // AiAnalysisService.BuildPrompt.
    private class ClaudeAnalysisPayload
    {
        [JsonPropertyName("root_cause")]
        public string RootCause { get; set; } = string.Empty;

        [JsonPropertyName("fix_suggestion")]
        public string FixSuggestion { get; set; } = string.Empty;
    }
}

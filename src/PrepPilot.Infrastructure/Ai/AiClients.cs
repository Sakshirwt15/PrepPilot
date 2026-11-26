using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Exceptions;

namespace PrepPilot.Infrastructure.Ai;

public class GroqOptions
{
    /// <summary>Free key from https://console.groq.com. Never commit it: use user-secrets.</summary>
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
}

public class AnthropicOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-5-5";
}

internal static class AiErrors
{
    public static string Describe(string provider, string keySetting, int status) => status switch
    {
        401 or 403 => $"{provider} rejected the API key. Check the '{keySetting}' setting.",
        429 => $"{provider} rate limit reached. Wait a minute and try again, or ask for fewer questions.",
        _ => $"{provider} returned an error ({status}). Please try again."
    };

    public static ExternalServiceException NotConfigured(string keySetting) =>
        new($"AI is not configured. Set the '{keySetting}' setting (see README).");

    public static ExternalServiceException Unreachable() =>
        new("Could not reach the AI service. Please try again.");

    public static ExternalServiceException Unexpected() =>
        new("The AI service returned an unexpected response.");
}

/// <summary>Groq (free tier) via its OpenAI-compatible chat completions API.</summary>
public class GroqAiClient(HttpClient http, IOptions<GroqOptions> options) : IAiClient
{
    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, bool expectJson, CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw AiErrors.NotConfigured("Groq:ApiKey");

        object body = expectJson
            ? new
            {
                model = settings.Model,
                temperature = 0.7,
                max_tokens = 6000,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            }
            : new
            {
                model = settings.Model,
                temperature = 0.5,
                max_tokens = 2000,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

        using var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw AiErrors.Unreachable();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceException(AiErrors.Describe("Groq", "Groq:ApiKey", (int)response.StatusCode));

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                       ?? string.Empty;
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                throw AiErrors.Unexpected();
            }
        }
    }
}

/// <summary>Optional paid provider: Anthropic (Claude) Messages API. Enable with Ai:Provider = Anthropic.</summary>
public class AnthropicAiClient(HttpClient http, IOptions<AnthropicOptions> options) : IAiClient
{
    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, bool expectJson, CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw AiErrors.NotConfigured("Anthropic:ApiKey");

        var body = new
        {
            model = settings.Model,
            max_tokens = expectJson ? 8000 : 2000,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages") { Content = JsonContent.Create(body) };
        request.Headers.Add("x-api-key", settings.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw AiErrors.Unreachable();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceException(AiErrors.Describe("Anthropic", "Anthropic:ApiKey", (int)response.StatusCode));

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                throw AiErrors.Unexpected();
            }
        }
    }
}

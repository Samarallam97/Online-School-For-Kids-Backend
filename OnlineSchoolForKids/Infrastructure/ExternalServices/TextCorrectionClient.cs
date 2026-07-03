using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices;

public class TextCorrectionClient : ITextCorrectionClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TextCorrectionClient> _logger;

    public TextCorrectionClient(HttpClient http, IConfiguration config, ILogger<TextCorrectionClient> logger)
    {
        _http = http;
        _logger = logger;
        _http.BaseAddress = new Uri(config["TextCorrectionApi:BaseUrl"]
            ?? "https://text-correction-api.example.com");
        _http.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<TextCorrectionResult> CorrectAsync(string transcript, CancellationToken ct)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/correct-text", new { transcript }, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Text correction API returned {Status}: {Body}", resp.StatusCode, body);
                return new TextCorrectionResult { Success = false, Error = $"Correction API error: {resp.StatusCode}" };
            }

            var data = await resp.Content.ReadFromJsonAsync<CorrectionApiResponse>(cancellationToken: ct);
            if (data == null)
                return new TextCorrectionResult { Success = false, Error = "Empty response from correction API" };

            return new TextCorrectionResult
            {
                Success = true,
                Language = data.Language ?? string.Empty,
                Errors = data.Errors ?? new List<string>(),
                CorrectedText = data.CorrectedText ?? string.Empty,
                Accuracy = data.Accuracy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling text correction API");
            return new TextCorrectionResult { Success = false, Error = "Failed to reach the correction service" };
        }
    }

    private class CorrectionApiResponse
    {
        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        [JsonPropertyName("corrected_text")]
        public string? CorrectedText { get; set; }

        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }
    }
}
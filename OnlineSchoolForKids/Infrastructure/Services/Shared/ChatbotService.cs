using Domain.Interfaces.Services.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services.Shared
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;

        private const string PredictEndpoint = "/predict";
        private const string AddFaqEndpoint = "/admin/faq";

        public ChatbotService(HttpClient httpClient, ILogger<ChatbotService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // ── Ask ───────────────────────────────────────────────────────────────────

        public async Task<ChatbotResponse> AskAsync(
            string query, string? lang = null, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    PredictEndpoint, new PredictRequest(query, lang), ct);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<PredictResponse>(
                    cancellationToken: ct);

                if (result is null) return Fallback();

                return new ChatbotResponse(
                    result.Status,
                    result.Answer ?? string.Empty,
                    result.Similarity,
                    result.Language ?? lang ?? "en",
                    result.ResponseTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot AskAsync failed for query: {Query}", query);
                return Fallback();
            }
        }

        // ── Push to knowledge base ────────────────────────────────────────────────

        public async Task<bool> AddToKnowledgeBaseAsync(
            string questionAr, string answerAr,
            string questionEn, string answerEn,
            string category,
            CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(AddFaqEndpoint,
                    new AddFaqRequest(questionAr, answerAr, questionEn, answerEn, category), ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Chatbot knowledge base push returned {Status} for question: {Q}",
                        response.StatusCode, questionEn);
                    return false;
                }

                _logger.LogInformation(
                    "Q&A pushed to chatbot knowledge base: \"{Q}\"", questionEn);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to push Q&A to chatbot knowledge base: \"{Q}\"", questionEn);
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static ChatbotResponse Fallback() => new(
            Status: false,
            Answer: "The chatbot service is currently unavailable. Please try again later.",
            Similarity: 0, Language: "en", ResponseTime: 0);
    }

    // ── Wire shapes ───────────────────────────────────────────────────────────────

    internal record PredictRequest(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("lang")] string? Lang);

    internal record PredictResponse(
        [property: JsonPropertyName("status")] bool Status,
        [property: JsonPropertyName("answer")] string? Answer,
        [property: JsonPropertyName("similarity")] double Similarity,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("response_time")] double ResponseTime);

    internal record AddFaqRequest(
        [property: JsonPropertyName("question_ar")] string QuestionAr,
        [property: JsonPropertyName("answer_ar")] string AnswerAr,
        [property: JsonPropertyName("question_en")] string QuestionEn,
        [property: JsonPropertyName("answer_en")] string AnswerEn,
        [property: JsonPropertyName("category")] string Category);

}

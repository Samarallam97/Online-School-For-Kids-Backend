using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Commands.Course;


public class GenerateQuizCommand : IRequest<GenerateQuizResponse>
{
    public string LessonName { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    /// <summary>"easy" | "medium" | "hard"</summary>
    public string Difficulty { get; set; } = "easy";
    public int NumQuestions { get; set; } = 5;
}

public class GenerateQuizResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<QuizQuestionDto> Questions { get; set; } = new();
}

public class QuizQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }   // 0-based index
    public string Explanation { get; set; } = string.Empty;
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class GenerateQuizHandler : IRequestHandler<GenerateQuizCommand, GenerateQuizResponse>
{
    private readonly IConfiguration _config;
    private readonly ILogger<GenerateQuizHandler> _logger;

    private string QuizApiBaseUrl => _config["QuizApi:BaseUrl"]
        ?? "https://quiz-production-api.up.railway.app";

    public GenerateQuizHandler(IConfiguration config, ILogger<GenerateQuizHandler> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<GenerateQuizResponse> Handle(
        GenerateQuizCommand request, CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

            // POST /generate-quiz
            var payload = new
            {
                lesson_name = request.LessonName,
                transcript = request.Transcript,
                difficulty = request.Difficulty,
                q_type = "MCQ",
                num_q = request.NumQuestions
            };

            var resp = await http.PostAsJsonAsync($"{QuizApiBaseUrl}/generate-quiz", payload, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Quiz API returned {Status}: {Error}", resp.StatusCode, err);
                return Fail($"Quiz API error: {resp.StatusCode}");
            }

            // The API returns a raw JSON string (the quiz name/id) but the actual
            // questions come back as a structured list. Parse defensively.
            var raw = await resp.Content.ReadAsStringAsync(ct);

            // Try to parse as array of question objects
            List<QuizApiQuestion>? apiQuestions = null;
            try
            {
                // Some versions return the questions directly as an array
                apiQuestions = JsonSerializer.Deserialize<List<QuizApiQuestion>>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                // Others wrap them; try a wrapper object
                try
                {
                    var wrapper = JsonSerializer.Deserialize<QuizApiWrapper>(raw,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    apiQuestions = wrapper?.Questions;
                }
                catch { /* fall through to Fail */ }
            }

            if (apiQuestions == null || apiQuestions.Count == 0)
                return Fail("Quiz API returned no questions");

            var questions = apiQuestions.Select(q => new QuizQuestionDto
            {
                Question      = q.Question ?? string.Empty,
                Options       = q.Options   ?? new List<string>(),
                CorrectAnswer = q.CorrectAnswer,
                Explanation   = q.Explanation ?? string.Empty
            }).ToList();

            return new GenerateQuizResponse
            {
                Success   = true,
                Message   = $"Generated {questions.Count} questions",
                Questions = questions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quiz for lesson {Lesson}", request.LessonName);
            return Fail("An error occurred generating the quiz");
        }
    }

    private static GenerateQuizResponse Fail(string msg) =>
        new() { Success = false, Message = msg };

    // ── Internal DTOs mirroring the Quiz API response ─────────────────────────

    private class QuizApiQuestion
    {
        [JsonPropertyName("question")]
        public string? Question { get; set; }

        [JsonPropertyName("options")]
        public List<string>? Options { get; set; }

        [JsonPropertyName("correct_answer")]
        public int CorrectAnswer { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    private class QuizApiWrapper
    {
        [JsonPropertyName("questions")]
        public List<QuizApiQuestion>? Questions { get; set; }
    }
}
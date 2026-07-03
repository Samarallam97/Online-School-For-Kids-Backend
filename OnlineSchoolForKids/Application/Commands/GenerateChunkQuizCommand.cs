using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Application.Commands;

public class GenerateChunkQuizCommand : IRequest<GenerateChunkQuizResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public int NumQuestions { get; set; } = 5;
}

public class GenerateChunkQuizResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<DraftQuizSetDto> Quizzes { get; set; } = new();
}

public class DraftQuizSetDto
{
    public string Difficulty { get; set; } = string.Empty;
    public List<DraftQuizQuestionDto> Questions { get; set; } = new();
}

public class DraftQuizQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class GenerateChunkQuizHandler : IRequestHandler<GenerateChunkQuizCommand, GenerateChunkQuizResponse>
{
    private static readonly string[] Difficulties = { "easy", "medium", "hard" };

    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<GenerateChunkQuizHandler> _logger;

    private string QuizApiBaseUrl => _config["QuizApi:BaseUrl"]
        ?? "https://habiba-elshrkawy-quiz-generator.hf.space";

    public GenerateChunkQuizHandler(
        IVideoProcessingJobRepository jobRepo,
        IConfiguration config,
        ILogger<GenerateChunkQuizHandler> logger)
    {
        _jobRepo = jobRepo;
        _config  = config;
        _logger  = logger;
    }

    public async Task<GenerateChunkQuizResponse> Handle(GenerateChunkQuizCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId)
                return Fail("Job not found");

            var chunk = job.Chunks.FirstOrDefault(c => c.Id == request.ChunkId);
            if (chunk == null) return Fail("Chunk not found");
            if (chunk.IsSaved) return Fail("This chunk is already saved as a lesson");
            if (string.IsNullOrWhiteSpace(chunk.Transcript)) return Fail("Chunk has no transcript yet");

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

            var drafts = new List<Domain.Entities.Content.DraftQuizSet>();
            foreach (var difficulty in Difficulties)
            {
                var questions = await CallQuizApi(http, chunk.Title, chunk.Transcript, difficulty, request.NumQuestions, ct);
                if (questions == null)
                    return Fail($"Failed to generate {difficulty} quiz for this chunk");

                drafts.Add(new Domain.Entities.Content.DraftQuizSet
                {
                    Difficulty = difficulty,
                    Questions = questions
                });
            }

            chunk.DraftQuizzes = drafts;
            await _jobRepo.UpdateAsync(job.Id, job, ct);

            _logger.LogInformation("Generated quiz drafts for chunk {ChunkId} (job {JobId})", chunk.Id, job.Id);

            return new GenerateChunkQuizResponse
            {
                Success = true,
                Message = "Quizzes generated",
                Quizzes = drafts.Select(d => new DraftQuizSetDto
                {
                    Difficulty = d.Difficulty,
                    Questions = d.Questions.Select(q => new DraftQuizQuestionDto
                    {
                        Question = q.Question,
                        Options = q.Options,
                        CorrectAnswer = q.CorrectAnswer,
                        Explanation = q.Explanation
                    }).ToList()
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chunk quiz for chunk {ChunkId}", request.ChunkId);
            return Fail("An error occurred while generating quizzes");
        }
    }

    private async Task<List<Domain.Entities.Content.DraftQuizQuestion>?> CallQuizApi(
        HttpClient http, string lessonName, string transcript, string difficulty, int numQuestions, CancellationToken ct)
    {
        var payload = new
        {
            lesson_name = lessonName,
            transcript,
            difficulty,
            q_type = "MCQ",
            num_q = numQuestions
        };

        var resp = await http.PostAsJsonAsync($"{QuizApiBaseUrl}/generate-quiz", payload, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Quiz API returned {Status}: {Error}", resp.StatusCode, err);
            return null;
        }

        var raw = await resp.Content.ReadAsStringAsync(ct);

        List<QuizApiQuestion>? apiQuestions = null;
        try
        {
            apiQuestions = System.Text.Json.JsonSerializer.Deserialize<List<QuizApiQuestion>>(raw,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            try
            {
                var wrapper = System.Text.Json.JsonSerializer.Deserialize<QuizApiWrapper>(raw,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                apiQuestions = wrapper?.Questions;
            }
            catch { /* fall through to null */ }
        }

        if (apiQuestions == null || apiQuestions.Count == 0)
            return null;

        return apiQuestions.Select(q => new Domain.Entities.Content.DraftQuizQuestion
        {
            Question      = q.Question ?? string.Empty,
            Options       = q.Options   ?? new List<string>(),
            CorrectAnswer = q.Options != null && q.CorrectAnswer != null
          ? q.Options.IndexOf(q.CorrectAnswer)
          : -1,
            Explanation   = q.Explanation ?? string.Empty
        }).ToList();
    }

    private static GenerateChunkQuizResponse Fail(string msg) => new() { Success = false, Message = msg };

    private class QuizApiQuestion
    {
        [JsonPropertyName("question")] public string? Question { get; set; }
        [JsonPropertyName("options")] public List<string>? Options { get; set; }
        [JsonPropertyName("correct_answer")] public string? CorrectAnswer { get; set; }
        [JsonPropertyName("explanation")] public string? Explanation { get; set; }
    }

    private class QuizApiWrapper
    {
        [JsonPropertyName("questions")] public List<QuizApiQuestion>? Questions { get; set; }
    }
}
using Domain.Entities.Content.Progress;
using Domain.Entities.Content.Quizes;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Application.Commands;

public class SaveChunkAsLessonCommand : IRequest<SaveChunkAsLessonResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;

    // Final, creator-confirmed values (allows last-second tweaks at save time)
    public string Title { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsFree { get; set; }
}

public class SaveChunkAsLessonResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LessonId { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────

public class SaveChunkAsLessonHandler
    : IRequestHandler<SaveChunkAsLessonCommand, SaveChunkAsLessonResponse>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<SaveChunkAsLessonHandler> _logger;

    public SaveChunkAsLessonHandler(
        IVideoProcessingJobRepository jobRepo,
        ICourseRepository courseRepo,
        ILogger<SaveChunkAsLessonHandler> logger)
    {
        _jobRepo = jobRepo;
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<SaveChunkAsLessonResponse> Handle(
        SaveChunkAsLessonCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId)
                return Fail("Job not found");

            var chunk = job.Chunks.FirstOrDefault(c => c.Id == request.ChunkId);
            if (chunk == null) return Fail("Chunk not found");
            if (chunk.IsSaved) return Fail("This chunk has already been saved");

            // A lesson must have a generated, reviewed quiz before it can be saved —
            // this is the enforcement point for "quiz generated per chunk before save".
            if (chunk.DraftQuizzes.Count == 0 || chunk.DraftQuizzes.All(q => q.Questions.Count == 0))
                return Fail("Generate a quiz for this chunk before saving it as a lesson");

            var course = await _courseRepo.GetByIdAsync(job.CourseId, ct);
            if (course == null) return Fail("Course not found");

            var section = course.Sections?.FirstOrDefault(s => s.Id == job.SectionId);
            if (section == null) return Fail("Section not found");

            var lesson = new Lesson
            {
                Id = ObjectId.GenerateNewId().ToString(),
                CourseId = job.CourseId,
                SectionId = job.SectionId,
                Title = request.Title,
                Description = request.Transcript,
                Duration = 0,
                Order = request.Order,
                VideoUrl = job.VideoUrl ?? string.Empty,
                IsFree = request.IsFree,
                IsPublished = true,
                Materials = new List<Material>(),
                Quizzes = chunk.DraftQuizzes.Select(d => new LessonQuiz
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Difficulty = d.Difficulty,
                    Questions = d.Questions.Select(qq => new QuizQuestion
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Text = qq.Question,
                        Options = qq.Options.Select((o, index) => new QuizOption
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Text = o,
                            IsCorrect = index == qq.CorrectAnswer,
                            Order = index
                        }).ToList(),
                        CorrectAnswer = qq.CorrectAnswer,
                        Explanation = qq.Explanation
                    }).ToList()
                }).ToList()
            };

            section.Lessons ??= new List<Lesson>();
            section.Lessons.Add(lesson);
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepo.UpdateAsync(course.Id, course, ct);

            chunk.IsSaved = true;
            chunk.LessonId = lesson.Id;
            chunk.Title = request.Title;
            chunk.Transcript = request.Transcript;
            await _jobRepo.UpdateAsync(job.Id, job, ct);

            _logger.LogInformation(
                "Chunk {ChunkId} saved as lesson {LessonId} with {Count} quiz sets",
                chunk.Id, lesson.Id, lesson.Quizzes.Count);

            return new SaveChunkAsLessonResponse
            {
                Success = true,
                Message = "Lesson created from chunk",
                LessonId = lesson.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chunk {ChunkId} as lesson", request.ChunkId);
            return Fail("An error occurred");
        }
    }

    private static SaveChunkAsLessonResponse Fail(string msg) =>
        new() { Success = false, Message = msg };
}
using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Application.Commands;

public class SaveReviewedChunkCommand : IRequest<SaveReviewedChunkResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;

    // Instructor-edited values
    public string Title { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;

    // Where to put this lesson
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class SaveReviewedChunkResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LessonId { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────

public class SaveReviewedChunkHandler
    : IRequestHandler<SaveReviewedChunkCommand, SaveReviewedChunkResponse>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<SaveReviewedChunkHandler> _logger;

    public SaveReviewedChunkHandler(
        IVideoProcessingJobRepository jobRepo,
        ICourseRepository courseRepo,
        ILogger<SaveReviewedChunkHandler> logger)
    {
        _jobRepo = jobRepo;
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<SaveReviewedChunkResponse> Handle(
        SaveReviewedChunkCommand request, CancellationToken ct)
    {
        try
        {
            // 1. Load job and verify ownership
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId)
                return Fail("Job not found");

            var chunk = job.Chunks.FirstOrDefault(c => c.Id == request.ChunkId);
            if (chunk == null)
                return Fail("Chunk not found");

            // 2. Load course
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null)
                return Fail("Course not found");

            var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
            if (section == null)
                return Fail("Section not found");

            // 3. Create lesson inside the section
            var lesson = new Lesson
            {
                Id = ObjectId.GenerateNewId().ToString(),
                CourseId = request.CourseId,
                SectionId = request.SectionId,
                Title = request.Title,
                Description = request.Transcript,   // transcript stored as description
                Duration = 0,
                Order = request.Order,
                VideoUrl = string.Empty,
                IsFree = false,
                Materials = new List<Material>()
            };

            section.Lessons ??= new List<Lesson>();
            section.Lessons.Add(lesson);
            course.UpdatedAt = DateTime.UtcNow;

            await _courseRepo.UpdateAsync(course.Id, course, ct);

            // 4. Mark chunk as saved
            chunk.IsSaved = true;
            chunk.LessonId = lesson.Id;
            chunk.SectionId = request.SectionId;
            chunk.LessonTitle = request.Title;

            // Update transcript on chunk (instructor may have edited it)
            chunk.Transcript = request.Transcript;
            chunk.Title = request.Title;

            await _jobRepo.UpdateAsync(job.Id, job, ct);

            _logger.LogInformation(
                "Chunk {ChunkId} saved as lesson {LessonId} in section {SectionId}",
                chunk.Id, lesson.Id, request.SectionId);

            return new SaveReviewedChunkResponse
            {
                Success = true,
                Message = "Lesson created from chunk",
                LessonId = lesson.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reviewed chunk {ChunkId}", request.ChunkId);
            return Fail("An error occurred");
        }
    }

    private static SaveReviewedChunkResponse Fail(string msg) =>
        new() { Success = false, Message = msg };
}
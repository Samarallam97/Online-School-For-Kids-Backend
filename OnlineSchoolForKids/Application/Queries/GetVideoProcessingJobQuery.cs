using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries;

public class GetVideoProcessingJobQuery : IRequest<VideoProcessingJobDto?>
{
    public string JobId { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
}

// ── Handler ───────────────────────────────────────────────────────────────

public class GetVideoProcessingJobHandler
    : IRequestHandler<GetVideoProcessingJobQuery, VideoProcessingJobDto?>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ILogger<GetVideoProcessingJobHandler> _logger;

    public GetVideoProcessingJobHandler(
        IVideoProcessingJobRepository jobRepo,
        ILogger<GetVideoProcessingJobHandler> logger)
    {
        _jobRepo = jobRepo;
        _logger = logger;
    }

    public async Task<VideoProcessingJobDto?> Handle(
        GetVideoProcessingJobQuery request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId)
                return null;

            return new VideoProcessingJobDto
            {
                Id = job.Id,
                CourseId = job.CourseId,
                SourceType = job.SourceType,
                SourceUrl = job.SourceUrl,
                Status = job.Status,
                ErrorMessage = job.ErrorMessage,
                RawTranscript = job.RawTranscript,
                Chunks = job.Chunks.Select(c => new VideoChunkDto
                {
                    Id = c.Id,
                    Index = c.Index,
                    Title = c.Title,
                    Summary = c.Summary,
                    Transcript = c.Transcript,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    SectionId = c.SectionId,
                    LessonTitle = c.LessonTitle,
                    IsSaved = c.IsSaved,
                    LessonId = c.LessonId
                }).OrderBy(c => c.Index).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video processing job {JobId}", request.JobId);
            return null;
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public class VideoProcessingJobDto
{
    public string Id { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? RawTranscript { get; set; }
    public List<VideoChunkDto> Chunks { get; set; } = new();
}

public class VideoChunkDto
{
    public string Id { get; set; } = string.Empty;
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? SectionId { get; set; }
    public string? LessonTitle { get; set; }
    public bool IsSaved { get; set; }
    public string? LessonId { get; set; }
}

using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;


namespace Application.Commands;
public class StartVideoProcessingCommand : IRequest<StartVideoProcessingResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;

    /// <summary>"upload" or "youtube"</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>YouTube URL (when SourceType == "youtube")</summary>
    public string? YoutubeUrl { get; set; }

    /// <summary>Raw video bytes (when SourceType == "upload")</summary>
    public Stream? VideoStream { get; set; }
    public string? FileName { get; set; }
}

public class StartVideoProcessingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? JobId { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────

public class StartVideoProcessingHandler
    : IRequestHandler<StartVideoProcessingCommand, StartVideoProcessingResponse>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<StartVideoProcessingHandler> _logger;

    private string PipelineBaseUrl => _config["VideoPipeline:BaseUrl"]
        ?? "https://web-production-12d4d.up.railway.app";

    public StartVideoProcessingHandler(
        IVideoProcessingJobRepository jobRepo,
        ICourseRepository courseRepo,
        IConfiguration config,
        ILogger<StartVideoProcessingHandler> logger)
    {
        _jobRepo = jobRepo;
        _courseRepo = courseRepo;
        _config = config;
        _logger = logger;
    }

    public async Task<StartVideoProcessingResponse> Handle(
        StartVideoProcessingCommand request,
        CancellationToken ct)
    {
        try
        {
            // 1. Validate course exists and belongs to instructor
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null)
                return Fail("Course not found");

            if (course.InstructorId != request.InstructorId)
                return Fail("Unauthorized");

            // 2. Create job record
            var job = new VideoProcessingJob
            {
                InstructorId = request.InstructorId,
                CourseId = request.CourseId,
                SourceType = request.SourceType,
                SourceUrl = request.YoutubeUrl ?? request.FileName ?? "",
                Status = "processing"
            };

            await _jobRepo.CreateAsync(job, ct);

            // 3. Call the pipeline API
            PipelineResult? result = null;

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(15);

            if (request.SourceType == "youtube")
            {
                result = await CallYoutubePipeline(http, request.YoutubeUrl!, ct);
            }
            else
            {
                result = await CallVideoPipeline(http, request.VideoStream!, request.FileName!, ct);
            }

            if (result == null || !result.Success)
            {
                job.Status = "failed";
                job.ErrorMessage = result?.Error ?? "Pipeline call failed";
                await _jobRepo.UpdateAsync(job.Id, job, ct);
                return Fail(job.ErrorMessage);
            }

            // 4. Store result on job
            job.RawTranscript = result.Transcript;
            job.Chunks = result.Segments.Select((s, i) => new VideoChunk
            {
                Index = i,
                Title = s.Title,
                Summary = s.Summary,
                Transcript = s.Text,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList();
            job.Status = "awaiting_review";

            await _jobRepo.UpdateAsync(job.Id, job, ct);

            _logger.LogInformation(
                "Video processing job {JobId} completed with {Count} chunks",
                job.Id, job.Chunks.Count);

            return new StartVideoProcessingResponse
            {
                Success = true,
                Message = "Processing complete",
                JobId = job.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            return Fail("An error occurred during processing");
        }
    }

    // ── Pipeline callers ─────────────────────────────────────────────────

    private async Task<PipelineResult?> CallYoutubePipeline(
        HttpClient http, string youtubeUrl, CancellationToken ct)
    {
        var payload = new { url = youtubeUrl };
        var resp = await http.PostAsJsonAsync(
            $"{PipelineBaseUrl}/pipeline/youtube", payload, ct);

        if (!resp.IsSuccessStatusCode)
            return new PipelineResult { Success = false, Error = $"Pipeline returned {resp.StatusCode}" };

        var data = await resp.Content.ReadFromJsonAsync<PipelineApiResponse>(cancellationToken: ct);
        return MapApiResponse(data);
    }

    private async Task<PipelineResult?> CallVideoPipeline(
        HttpClient http, Stream videoStream, string fileName, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(videoStream);
        form.Add(streamContent, "file", fileName);

        var resp = await http.PostAsync(
            $"{PipelineBaseUrl}/pipeline/video", form, ct);

        if (!resp.IsSuccessStatusCode)
            return new PipelineResult { Success = false, Error = $"Pipeline returned {resp.StatusCode}" };

        var data = await resp.Content.ReadFromJsonAsync<PipelineApiResponse>(cancellationToken: ct);
        return MapApiResponse(data);
    }

    private static PipelineResult MapApiResponse(PipelineApiResponse? data)
    {
        if (data == null)
            return new PipelineResult { Success = false, Error = "Empty pipeline response" };

        return new PipelineResult
        {
            Success = true,
            Transcript = data.Transcript,
            Segments = data.Segments ?? new()
        };
    }

    private static StartVideoProcessingResponse Fail(string message) =>
        new() { Success = false, Message = message };

    // ── Internal DTOs mirroring the pipeline API ─────────────────────────

    private class PipelineApiResponse
    {
        public string? Transcript { get; set; }
        public List<PipelineSegment>? Segments { get; set; }
    }

    private class PipelineSegment
    {
        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    private class PipelineResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Transcript { get; set; }
        public List<PipelineSegment> Segments { get; set; } = new();
    }
}
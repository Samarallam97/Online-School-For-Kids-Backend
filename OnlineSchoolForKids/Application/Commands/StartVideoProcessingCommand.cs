using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Services.Shared;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Application.Commands;

public class StartVideoProcessingCommand : IRequest<StartVideoProcessingResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;

    /// <summary>"upload" or "youtube"</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>"chunked" (long content → many lessons) or "single" (short content → one lesson)</summary>
    public string Mode { get; set; } = "chunked";

    /// <summary>YouTube URL (when SourceType == "youtube")</summary>
    public string? YoutubeUrl { get; set; }

    /// <summary>Raw video stream (when SourceType == "upload")</summary>
    public Stream? VideoStream { get; set; }
    public string? FileName { get; set; }
}

public class StartVideoProcessingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? JobId { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class StartVideoProcessingHandler
    : IRequestHandler<StartVideoProcessingCommand, StartVideoProcessingResponse>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _config;
    private readonly ILogger<StartVideoProcessingHandler> _logger;

    private string PipelineBaseUrl =>
        _config["VideoPipeline:BaseUrl"] ?? "https://web-production-12d4d.up.railway.app";

    public StartVideoProcessingHandler(
        IVideoProcessingJobRepository jobRepo,
        ICourseRepository courseRepo,
        IFileStorageService fileStorage,
        IConfiguration config,
        ILogger<StartVideoProcessingHandler> logger)
    {
        _jobRepo     = jobRepo;
        _courseRepo  = courseRepo;
        _fileStorage = fileStorage;
        _config      = config;
        _logger      = logger;
    }

    public async Task<StartVideoProcessingResponse> Handle(
        StartVideoProcessingCommand request, CancellationToken ct)
    {
        try
        {
            // 1. Validate course ownership + section existence
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null) return Fail("Course not found");
            if (course.InstructorId != request.InstructorId) return Fail("Unauthorized");

            var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
            if (section == null) return Fail("Section not found");

            // 2. Create job record
            var job = new VideoProcessingJob
            {
                InstructorId = request.InstructorId,
                CourseId     = request.CourseId,
                SectionId    = request.SectionId,
                SourceType   = request.SourceType,
                Mode         = request.Mode,
                SourceUrl    = request.YoutubeUrl ?? request.FileName ?? "",
                Status       = "processing"
            };
            await _jobRepo.CreateAsync(job, ct);

            // 3. For uploads, persist the video file via storage BEFORE sending it to
            // the pipeline, since the pipeline consumes (and doesn't return) the
            // stream. We read it into memory once, then hand out two independent
            // streams — one to storage, one to the pipeline call.
            byte[]? videoBytes = null;
            if (request.SourceType == "upload" && request.VideoStream != null)
            {
                using var memoryStream = new MemoryStream();
                await request.VideoStream.CopyToAsync(memoryStream, ct);
                videoBytes = memoryStream.ToArray();

                using var storageStream = new MemoryStream(videoBytes);
                job.VideoUrl = await _fileStorage.UploadFileAsync(
                    storageStream, request.FileName ?? "video.mp4", "course-videos");
            }

            // 4. Call the right AI pipeline endpoint based on Mode + SourceType
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };

            PipelineResult? result = (request.Mode, request.SourceType) switch
            {
                ("chunked", "youtube") => await CallYoutubePipeline(http, request.YoutubeUrl!, ct),
                ("chunked", "upload") => await CallVideoPipeline(http, videoBytes!, request.FileName!, ct),
                ("single", "upload") => await CallTranscribeOnly(http, videoBytes!, request.FileName!, ct),
                // No transcript-only YouTube endpoint exists on the pipeline API, so a
                // short YouTube lesson still runs the full pipeline; we simply keep the
                // transcript and discard segments/description for single-lesson mode.
                ("single", "youtube") => await CallYoutubePipeline(http, request.YoutubeUrl!, ct),
                _ => new PipelineResult { Success = false, Error = "Unsupported source/mode combination" }
            };

            if (result == null || !result.Success)
            {
                job.Status       = "failed";
                job.ErrorMessage = result?.Error ?? "Pipeline call failed";
                await _jobRepo.UpdateAsync(job.Id, job, ct);
                return Fail(job.ErrorMessage);
            }

            // 5. Store transcript (always)
            job.RawTranscript = result.Transcript;

            if (request.Mode == "chunked")
            {
                if (result.Description != null)
                {
                    job.Description = new PipelineDescription
                    {
                        Summary        = result.Description.Summary        ?? string.Empty,
                        TargetAudience = result.Description.TargetAudience ?? string.Empty,
                        ToneAndStyle   = result.Description.ToneAndStyle   ?? string.Empty,
                        SeoTags        = result.Description.SeoTags        ?? new List<string>()
                    };
                }

                job.Chunks = result.Segments.Select((s, i) => new VideoChunk
                {
                    Index      = i,
                    Title      = s.Title,
                    Summary    = s.Summary,
                    Transcript = s.Text,
                    StartTime  = s.StartTime,
                    EndTime    = s.EndTime
                }).ToList();
            }
            else // "single" mode: exactly one synthetic chunk covering the whole transcript
            {
                job.Chunks = new List<VideoChunk>
                {
                    new VideoChunk
                    {
                        Index      = 0,
                        Title      = request.FileName ?? "Lesson",
                        Summary    = string.Empty,
                        Transcript = result.Transcript ?? string.Empty,
                        StartTime  = "00:00:00",
                        EndTime    = string.Empty
                    }
                };
            }

            job.Status = "awaiting_correction";
            await _jobRepo.UpdateAsync(job.Id, job, ct);

            _logger.LogInformation(
                "Video processing job {JobId} completed (mode: {Mode}, chunks: {Count})",
                job.Id, job.Mode, job.Chunks.Count);

            return new StartVideoProcessingResponse
            {
                Success = true,
                Message = "Processing complete",
                JobId   = job.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video");
            return Fail("An error occurred during processing");
        }
    }

    // ── Pipeline callers ──────────────────────────────────────────────────────

    private async Task<PipelineResult?> CallYoutubePipeline(
        HttpClient http, string youtubeUrl, CancellationToken ct)
    {
        var payload = new { url = youtubeUrl };
        var resp = await http.PostAsJsonAsync($"{PipelineBaseUrl}/pipeline/youtube", payload, ct);

        if (!resp.IsSuccessStatusCode)
            return new PipelineResult { Success = false, Error = $"Pipeline returned {resp.StatusCode}" };

        var data = await resp.Content.ReadFromJsonAsync<PipelineApiResponse>(cancellationToken: ct);
        return MapApiResponse(data);
    }

    private async Task<PipelineResult?> CallVideoPipeline(
        HttpClient http, byte[] videoBytes, string fileName, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(new MemoryStream(videoBytes));
        form.Add(streamContent, "file", fileName);

        var resp = await http.PostAsync($"{PipelineBaseUrl}/pipeline/video", form, ct);

        if (!resp.IsSuccessStatusCode)
            return new PipelineResult { Success = false, Error = $"Pipeline returned {resp.StatusCode}" };

        var data = await resp.Content.ReadFromJsonAsync<PipelineApiResponse>(cancellationToken: ct);
        return MapApiResponse(data);
    }

    /// <summary>Transcript-only call for short single-lesson uploads — skips segmentation/description.</summary>
    private async Task<PipelineResult?> CallTranscribeOnly(
        HttpClient http, byte[] videoBytes, string fileName, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(new MemoryStream(videoBytes));
        form.Add(streamContent, "file", fileName);

        var resp = await http.PostAsync($"{PipelineBaseUrl}/transcribe/video", form, ct);

        if (!resp.IsSuccessStatusCode)
            return new PipelineResult { Success = false, Error = $"Pipeline returned {resp.StatusCode}" };

        var data = await resp.Content.ReadFromJsonAsync<TranscribeApiResponse>(cancellationToken: ct);
        if (data == null)
            return new PipelineResult { Success = false, Error = "Empty pipeline response" };

        return new PipelineResult { Success = true, Transcript = data.Transcript };
    }

    private static PipelineResult MapApiResponse(PipelineApiResponse? data)
    {
        if (data == null)
            return new PipelineResult { Success = false, Error = "Empty pipeline response" };

        return new PipelineResult
        {
            Success     = true,
            Transcript  = data.Transcript,
            Segments    = data.Segments    ?? new(),
            Description = data.Description
        };
    }

    private static StartVideoProcessingResponse Fail(string message) =>
        new() { Success = false, Message = message };

    // ── DTOs mirroring the pipeline API response ──────────────────────────────

    private class PipelineApiResponse
    {
        [JsonPropertyName("transcript")]
        public string? Transcript { get; set; }

        [JsonPropertyName("segments")]
        public List<PipelineSegment>? Segments { get; set; }

        [JsonPropertyName("description")]
        public PipelineDescriptionApi? Description { get; set; }
    }

    private class TranscribeApiResponse
    {
        [JsonPropertyName("transcript")]
        public string? Transcript { get; set; }
    }

    private class PipelineSegment
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = string.Empty;
    }

    private class PipelineDescriptionApi
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("target_audience")]
        public string? TargetAudience { get; set; }

        [JsonPropertyName("tone_and_style")]
        public string? ToneAndStyle { get; set; }

        [JsonPropertyName("seo_tags")]
        public List<string>? SeoTags { get; set; }
    }

    private class PipelineResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Transcript { get; set; }
        public List<PipelineSegment> Segments { get; set; } = new();
        public PipelineDescriptionApi? Description { get; set; }
    }
}
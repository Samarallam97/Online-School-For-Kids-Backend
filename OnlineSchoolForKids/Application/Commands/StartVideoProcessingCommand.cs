using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands;

public class StartVideoProcessingCommand : IRequest<StartVideoProcessingResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;

    /// <summary>"upload" or "youtube"</summary>
    public string SourceType { get; set; } = string.Empty;

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
    private readonly IConfiguration _config;
    private readonly ILogger<StartVideoProcessingHandler> _logger;

    private string PipelineBaseUrl =>
        _config["VideoPipeline:BaseUrl"] ?? "https://web-production-12d4d.up.railway.app";

    public StartVideoProcessingHandler(
        IVideoProcessingJobRepository jobRepo,
        ICourseRepository courseRepo,
        IConfiguration config,
        ILogger<StartVideoProcessingHandler> logger)
    {
        _jobRepo    = jobRepo;
        _courseRepo = courseRepo;
        _config     = config;
        _logger     = logger;
    }

    public async Task<StartVideoProcessingResponse> Handle(
        StartVideoProcessingCommand request, CancellationToken ct)
    {
        try
        {
            // 1. Validate course ownership
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null)
                return Fail("Course not found");
            if (course.InstructorId != request.InstructorId)
                return Fail("Unauthorized");

            // 2. Create job record
            var job = new VideoProcessingJob
            {
                InstructorId = request.InstructorId,
                CourseId     = request.CourseId,
                SourceType   = request.SourceType,
                SourceUrl    = request.YoutubeUrl ?? request.FileName ?? "",
                Status       = "processing"
            };
            await _jobRepo.CreateAsync(job, ct);

            // 3. Call the AI pipeline
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };

            PipelineResult? result = request.SourceType == "youtube"
                ? await CallYoutubePipeline(http, request.YoutubeUrl!, ct)
                : await CallVideoPipeline(http, request.VideoStream!, request.FileName!, ct);

            if (result == null || !result.Success)
            {
                job.Status       = "failed";
                job.ErrorMessage = result?.Error ?? "Pipeline call failed";
                await _jobRepo.UpdateAsync(job.Id, job, ct);
                return Fail(job.ErrorMessage);
            }

            // 4. Store everything returned by the pipeline
            job.RawTranscript = result.Transcript;

            // Pipeline description object (course-level metadata)
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

            // Video chunks / segments
            job.Chunks = result.Segments.Select((s, i) => new VideoChunk
            {
                Index     = i,
                Title     = s.Title,
                Summary   = s.Summary,
                Transcript= s.Text,
                StartTime = s.StartTime,
                EndTime   = s.EndTime
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
        HttpClient http, Stream videoStream, string fileName, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(videoStream);
        form.Add(streamContent, "file", fileName);

        var resp = await http.PostAsync($"{PipelineBaseUrl}/pipeline/video", form, ct);

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

    private class PipelineSegment
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>The pipeline returns chunk text in the "text" field.</summary>
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
using Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ContentCreator")]
public class VideoProcessingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VideoProcessingController> _logger;

    public VideoProcessingController(IMediator mediator, ILogger<VideoProcessingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ── Entry point 1 & 3: long video / long YouTube → chunked ──────────────

    /// <summary>POST /api/videoprocessing/chunked/youtube</summary>
    [HttpPost("chunked/youtube")]
    public async Task<IActionResult> StartChunkedFromYoutube(
        [FromBody] ProcessYoutubeRequest request,
        CancellationToken ct)
    {
        return await StartJob(new StartVideoProcessingCommand
        {
            InstructorId = GetUserId(),
            CourseId = request.CourseId,
            SectionId = request.SectionId,
            SourceType = "youtube",
            Mode = "chunked",
            YoutubeUrl = request.YoutubeUrl
        }, ct);
    }

    /// <summary>POST /api/videoprocessing/chunked/upload</summary>
    [HttpPost("chunked/upload")]
    [RequestSizeLimit(4L * 1024 * 1024 * 1024)] // 4 GB
    public async Task<IActionResult> StartChunkedFromUpload(
        [FromForm] string courseId,
        [FromForm] string sectionId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided", success = false });

        return await StartJob(new StartVideoProcessingCommand
        {
            InstructorId = GetUserId(),
            CourseId = courseId,
            SectionId = sectionId,
            SourceType = "upload",
            Mode = "chunked",
            VideoStream = file.OpenReadStream(),
            FileName = file.FileName
        }, ct);
    }

    // ── Entry point 2 & 4: small video / small YouTube → single lesson ──────

    /// <summary>POST /api/videoprocessing/single/youtube</summary>
    [HttpPost("single/youtube")]
    public async Task<IActionResult> StartSingleFromYoutube(
        [FromBody] ProcessYoutubeRequest request,
        CancellationToken ct)
    {
        return await StartJob(new StartVideoProcessingCommand
        {
            InstructorId = GetUserId(),
            CourseId = request.CourseId,
            SectionId = request.SectionId,
            SourceType = "youtube",
            Mode = "single",
            YoutubeUrl = request.YoutubeUrl
        }, ct);
    }

    /// <summary>POST /api/videoprocessing/single/upload</summary>
    [HttpPost("single/upload")]
    [RequestSizeLimit(4L * 1024 * 1024 * 1024)] // 4 GB
    public async Task<IActionResult> StartSingleFromUpload(
        [FromForm] string courseId,
        [FromForm] string sectionId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided", success = false });

        return await StartJob(new StartVideoProcessingCommand
        {
            InstructorId = GetUserId(),
            CourseId = courseId,
            SectionId = sectionId,
            SourceType = "upload",
            Mode = "single",
            VideoStream = file.OpenReadStream(),
            FileName = file.FileName
        }, ct);
    }

    private async Task<IActionResult> StartJob(StartVideoProcessingCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new { data = new { jobId = result.JobId }, message = result.Message, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting video processing job");
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    // ── Job retrieval ─────────────────────────────────────────────────────────

    /// <summary>GET /api/videoprocessing/{jobId}</summary>
    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(string jobId, CancellationToken ct)
    {
        try
        {
            var query = new Application.Queries.GetVideoProcessingJobQuery { JobId = jobId, InstructorId = GetUserId() };
            var result = await _mediator.Send(query, ct);

            if (result == null)
                return NotFound(new { message = "Job not found", success = false });

            return Ok(new { data = result, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {JobId}", jobId);
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    // ── Transcript correction (runs once, pre-chunking, on the raw transcript) ──

    /// <summary>POST /api/videoprocessing/{jobId}/correct-transcript — "Check accuracy" button</summary>
    [HttpPost("{jobId}/correct-transcript")]
    public async Task<IActionResult> CorrectTranscript(string jobId, CancellationToken ct)
    {
        try
        {
            var command = new CorrectTranscriptCommand { JobId = jobId, InstructorId = GetUserId() };
            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new
            {
                data = new
                {
                    correctedTranscript = result.CorrectedTranscript,
                    accuracy = result.Accuracy,
                    detectedLanguage = result.DetectedLanguage,
                    errors = result.Errors,
                    needsRevision = result.NeedsRevision
                },
                message = result.Message,
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error correcting transcript for job {JobId}", jobId);
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    /// <summary>POST /api/videoprocessing/{jobId}/approve-transcript — accept corrected or keep original</summary>
    [HttpPost("{jobId}/approve-transcript")]
    public async Task<IActionResult> ApproveTranscript(
        string jobId,
        [FromBody] ApproveTranscriptRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new ApproveTranscriptCommand
            {
                JobId = jobId,
                InstructorId = GetUserId(),
                Choice = request.Choice
            };
            var result = await _mediator.Send(command, ct);

            if (!result)
                return BadRequest(new { message = "Failed to approve transcript", success = false });

            return Ok(new { message = "Transcript approved", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving transcript for job {JobId}", jobId);
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    /// <summary>PATCH /api/videoprocessing/{jobId}/transcript — manual edit of the raw transcript (pre-chunking)</summary>
    [HttpPatch("{jobId}/transcript")]
    public async Task<IActionResult> UpdateRawTranscript(
        string jobId,
        [FromBody] UpdateRawTranscriptRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateRawTranscriptCommand
            {
                JobId = jobId,
                InstructorId = GetUserId(),
                Transcript = request.Transcript
            };
            var result = await _mediator.Send(command, ct);

            if (!result)
                return NotFound(new { message = "Job not found", success = false });

            return Ok(new { message = "Transcript updated", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transcript for job {JobId}", jobId);
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    // ── Chunk boundary / content editing ─────────────────────────────────────

    /// <summary>PATCH /api/videoprocessing/{jobId}/chunks/{chunkId} — edit a chunk's title/transcript/boundaries</summary>
    [HttpPatch("{jobId}/chunks/{chunkId}")]
    public async Task<IActionResult> UpdateChunk(
        string jobId,
        string chunkId,
        [FromBody] UpdateChunkRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateChunkCommand
            {
                InstructorId = GetUserId(),
                JobId = jobId,
                ChunkId = chunkId,
                Title = request.Title,
                Transcript = request.Transcript,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new
            {
                data = new
                {
                    transcript = result.Transcript,
                    needsTranscriptReview = result.NeedsTranscriptReview,
                    startAlignment = result.StartAlignment,
                    endAlignment = result.EndAlignment
                },
                message = result.Message,
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chunk");
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    // ── Per-chunk quiz generation + editing ───────────────────────────────────

    /// <summary>POST /api/videoprocessing/{jobId}/chunks/{chunkId}/generate-quiz</summary>
    [HttpPost("{jobId}/chunks/{chunkId}/generate-quiz")]
    public async Task<IActionResult> GenerateChunkQuiz(
        string jobId,
        string chunkId,
        [FromBody] GenerateChunkQuizRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new GenerateChunkQuizCommand
            {
                InstructorId = GetUserId(),
                JobId = jobId,
                ChunkId = chunkId,
                NumQuestions = request.NumQuestions
            };

            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new { data = result.Quizzes, message = result.Message, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quiz for chunk {ChunkId}", chunkId);
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    /// <summary>PUT /api/videoprocessing/{jobId}/chunks/{chunkId}/quiz/{difficulty} — edit draft questions</summary>
    [HttpPut("{jobId}/chunks/{chunkId}/quiz/{difficulty}")]
    public async Task<IActionResult> UpdateChunkQuizQuestions(
        string jobId,
        string chunkId,
        string difficulty,
        [FromBody] List<DraftQuizQuestionDto> questions,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateChunkQuizQuestionsCommand
            {
                InstructorId = GetUserId(),
                JobId = jobId,
                ChunkId = chunkId,
                Difficulty = difficulty,
                Questions = questions
            };

            var result = await _mediator.Send(command, ct);
            if (!result)
                return NotFound(new { message = "Chunk not found", success = false });

            return Ok(new { message = "Quiz questions updated", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quiz questions for chunk {ChunkId}", chunkId);
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    // ── Final save: chunk → real Lesson (quiz required) ──────────────────────

    /// <summary>POST /api/videoprocessing/{jobId}/chunks/{chunkId}/save</summary>
    [HttpPost("{jobId}/chunks/{chunkId}/save")]
    public async Task<IActionResult> SaveChunkAsLesson(
        string jobId,
        string chunkId,
        [FromBody] SaveChunkAsLessonRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new SaveChunkAsLessonCommand
            {
                InstructorId = GetUserId(),
                JobId = jobId,
                ChunkId = chunkId,
                Title = request.Title,
                Transcript = request.Transcript,
                Order = request.Order,
                IsFree = request.IsFree,
                Duration = request.Duration,   // ← add this line
            };

            var result = await _mediator.Send(command, ct);
            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new
            {
                data = new { lessonId = result.LessonId },
                message = result.Message,
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chunk as lesson");
            return StatusCode(500, new { message = "An error occurred", success = false });
        }
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}

// ── Request DTOs ──────────────────────────────────────────────────────────

public class ProcessYoutubeRequest
{
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string YoutubeUrl { get; set; } = string.Empty;
}

public class ApproveTranscriptRequest
{
    /// <summary>"corrected" | "original"</summary>
    public string Choice { get; set; } = "original";
}

public class UpdateRawTranscriptRequest
{
    public string Transcript { get; set; } = string.Empty;
}

public class UpdateChunkRequest
{
    public string? Title { get; set; }
    public string? Transcript { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}

public class GenerateChunkQuizRequest
{
    public int NumQuestions { get; set; } = 5;
}

public class SaveChunkAsLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsFree { get; set; }
    public int Duration { get; set; }   // ← add this line — seconds, from the frontend
}
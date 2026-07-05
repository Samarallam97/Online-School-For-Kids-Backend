using Application.Commands;
using Application.Queries;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ContentCreator")]
public class VideoProcessingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VideoProcessingController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public VideoProcessingController(IMediator mediator, ILogger<VideoProcessingController> logger, IStringLocalizer<SharedResource> localizer)
    {
        _mediator = mediator;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Start a video processing job from a YouTube URL.
    /// POST /api/videoprocessing/youtube
    /// Body: { courseId, youtubeUrl }
    /// </summary>
    [HttpPost("youtube")]
    public async Task<IActionResult> ProcessYoutube(
        [FromBody] ProcessYoutubeRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new StartVideoProcessingCommand
            {
                InstructorId = userId,
                CourseId = request.CourseId,
                SourceType = "youtube",
                YoutubeUrl = request.YoutubeUrl
            };

            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new { data = new { jobId = result.JobId }, message = result.Message, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing YouTube video");
            return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
        }
    }

    /// <summary>
    /// Start a video processing job from an uploaded file.
    /// POST /api/videoprocessing/upload
    /// Form: courseId (string), file (video file)
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(4L * 1024 * 1024 * 1024)] // 4 GB
    public async Task<IActionResult> ProcessUpload(
        [FromForm] string courseId,
        IFormFile file,
        CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = _localizer["NoFileProvided"], success = false });

            var command = new StartVideoProcessingCommand
            {
                InstructorId = userId,
                CourseId = courseId,
                SourceType = "upload",
                VideoStream = file.OpenReadStream(),
                FileName = file.FileName
            };

            var result = await _mediator.Send(command, ct);

            if (!result.Success)
                return BadRequest(new { message = result.Message, success = false });

            return Ok(new { data = new { jobId = result.JobId }, message = result.Message, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded video");
            return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
        }
    }

    /// <summary>
    /// Get a processing job (status + chunks) for review.
    /// GET /api/videoprocessing/{jobId}
    /// </summary>
    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(string jobId, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var query = new GetVideoProcessingJobQuery
            {
                JobId = jobId,
                InstructorId = userId
            };

            var result = await _mediator.Send(query, ct);

            if (result == null)
                return NotFound(new { message = _localizer["JobNotFound"], success = false });

            return Ok(new { data = result, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {JobId}", jobId);
            return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
        }
    }

    /// <summary>
    /// Update a chunk's title/transcript (auto-save during review).
    /// PATCH /api/videoprocessing/{jobId}/chunks/{chunkId}
    /// </summary>
    [HttpPatch("{jobId}/chunks/{chunkId}")]
    public async Task<IActionResult> UpdateChunk(
        string jobId,
        string chunkId,
        [FromBody] UpdateChunkRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new UpdateChunkCommand
            {
                InstructorId = userId,
                JobId = jobId,
                ChunkId = chunkId,
                Title = request.Title,
                Transcript = request.Transcript
            };

            var result = await _mediator.Send(command, ct);

            if (!result)
                return NotFound(new { message = _localizer["ChunkNotFound"], success = false });

            return Ok(new { message = _localizer["ChunkUpdated"], success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chunk");
            return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
        }
    }

    /// <summary>
    /// Save a reviewed chunk as a lesson inside a section.
    /// POST /api/videoprocessing/{jobId}/chunks/{chunkId}/save
    /// </summary>
    [HttpPost("{jobId}/chunks/{chunkId}/save")]
    public async Task<IActionResult> SaveChunk(
        string jobId,
        string chunkId,
        [FromBody] SaveChunkRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var command = new SaveReviewedChunkCommand
            {
                InstructorId = userId,
                JobId = jobId,
                ChunkId = chunkId,
                CourseId = request.CourseId,
                SectionId = request.SectionId,
                Title = request.Title,
                Transcript = request.Transcript,
                Order = request.Order
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
            return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
        }
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────

public class ProcessYoutubeRequest
{
    public string CourseId { get; set; } = string.Empty;
    public string YoutubeUrl { get; set; } = string.Empty;
}

public class UpdateChunkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
}

public class SaveChunkRequest
{
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public int Order { get; set; }
}

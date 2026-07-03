using Application.Common;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public class UpdateChunkCommand : IRequest<UpdateChunkResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public string? Title { get; set; }

    /// <summary>
    /// Manual transcript override. Ignored (recomputed instead) if StartTime
    /// or EndTime are also provided in this request, since boundary changes
    /// are the source of truth for transcript content.
    /// </summary>
    public string? Transcript { get; set; }

    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}

public class UpdateChunkResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Transcript { get; set; }

    /// <summary>
    /// True if either boundary was changed and landed between two transcript
    /// lines rather than exactly on one — the creator should review the
    /// resulting transcript text before generating a quiz from it.
    /// </summary>
    public bool NeedsTranscriptReview { get; set; }
    public BoundaryAlignment? StartAlignment { get; set; }
    public BoundaryAlignment? EndAlignment { get; set; }
}

public class UpdateChunkHandler : IRequestHandler<UpdateChunkCommand, UpdateChunkResponse>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ILogger<UpdateChunkHandler> _logger;

    public UpdateChunkHandler(
        IVideoProcessingJobRepository jobRepo,
        ILogger<UpdateChunkHandler> logger)
    {
        _jobRepo = jobRepo;
        _logger = logger;
    }

    public async Task<UpdateChunkResponse> Handle(UpdateChunkCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId)
                return Fail("Job not found");

            var chunk = job.Chunks.FirstOrDefault(c => c.Id == request.ChunkId);
            if (chunk == null) return Fail("Chunk not found");
            if (chunk.IsSaved) return Fail("This chunk has already been saved and can no longer be edited");

            bool boundariesChanged = request.StartTime != null || request.EndTime != null;

            if (request.Title != null) chunk.Title = request.Title;

            BoundaryAlignment? startAlignment = null;
            BoundaryAlignment? endAlignment = null;

            if (boundariesChanged)
            {
                var newStart = request.StartTime ?? chunk.StartTime;
                var newEnd = request.EndTime ?? chunk.EndTime;

                if (string.IsNullOrWhiteSpace(job.RawTranscript))
                    return Fail("This job has no raw transcript to re-slice from");

                chunk.StartTime = newStart;
                chunk.EndTime = newEnd;
                chunk.Transcript = TranscriptSlicer.Slice(job.RawTranscript, newStart, newEnd);

                var startSeconds = TranscriptSlicer.ParseTimeToSeconds(newStart) ?? 0;
                startAlignment = TranscriptSlicer.CheckAlignment(job.RawTranscript, startSeconds);

                var endSeconds = TranscriptSlicer.ParseTimeToSeconds(newEnd);
                if (endSeconds.HasValue)
                    endAlignment = TranscriptSlicer.CheckAlignment(job.RawTranscript, endSeconds.Value);
            }
            else if (request.Transcript != null)
            {
                // Pure manual text edit, boundaries untouched.
                chunk.Transcript = request.Transcript;
            }

            // Any transcript change (auto-resliced or manually edited) invalidates
            // previously generated quiz drafts — they no longer match the text.
            if (chunk.DraftQuizzes.Count > 0)
                chunk.DraftQuizzes.Clear();

            await _jobRepo.UpdateAsync(job.Id, job, ct);

            bool needsReview = (startAlignment?.IsAligned == false) || (endAlignment?.IsAligned == false);

            return new UpdateChunkResponse
            {
                Success = true,
                Message = needsReview
                    ? "Chunk updated — please review the transcript, the boundary fell between two lines"
                    : "Chunk updated",
                Transcript = chunk.Transcript,
                NeedsTranscriptReview = needsReview,
                StartAlignment = startAlignment,
                EndAlignment = endAlignment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chunk {ChunkId}", request.ChunkId);
            return Fail("An error occurred while updating the chunk");
        }
    }

    private static UpdateChunkResponse Fail(string msg) => new() { Success = false, Message = msg };
}
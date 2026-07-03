using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

/// <summary>
/// Lets the creator explicitly accept either the corrected transcript or keep
/// their original/edited version as final before moving on to chunking/quiz
/// generation. Required because corrections must never silently overwrite
/// creator-owned text.
/// </summary>
public class ApproveTranscriptCommand : IRequest<bool>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;

    /// <summary>"corrected" = adopt CorrectedTranscript as RawTranscript; "original" = keep RawTranscript as-is.</summary>
    public string Choice { get; set; } = "original";
}

public class ApproveTranscriptHandler : IRequestHandler<ApproveTranscriptCommand, bool>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ILogger<ApproveTranscriptHandler> _logger;

    public ApproveTranscriptHandler(
        IVideoProcessingJobRepository jobRepo,
        ILogger<ApproveTranscriptHandler> logger)
    {
        _jobRepo = jobRepo;
        _logger = logger;
    }

    public async Task<bool> Handle(ApproveTranscriptCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId) return false;

            if (request.Choice == "corrected" && !string.IsNullOrWhiteSpace(job.CorrectedTranscript))
            {
                job.RawTranscript = job.CorrectedTranscript;
            }

            job.IsTranscriptApproved = true;
            await _jobRepo.UpdateAsync(job.Id, job, ct);

            _logger.LogInformation("Transcript approved for job {JobId} (choice: {Choice})", job.Id, request.Choice);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving transcript for job {JobId}", request.JobId);
            return false;
        }
    }
}
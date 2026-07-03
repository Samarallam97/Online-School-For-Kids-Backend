using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

/// <summary>
/// Updates the job's raw transcript when the creator manually edits it
/// (before chunking). Clears any prior accuracy/approval state since the
/// text has changed and must be re-checked.
/// </summary>
public class UpdateRawTranscriptCommand : IRequest<bool>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
}

public class UpdateRawTranscriptHandler : IRequestHandler<UpdateRawTranscriptCommand, bool>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ILogger<UpdateRawTranscriptHandler> _logger;

    public UpdateRawTranscriptHandler(
        IVideoProcessingJobRepository jobRepo,
        ILogger<UpdateRawTranscriptHandler> logger)
    {
        _jobRepo = jobRepo;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateRawTranscriptCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId) return false;

            job.RawTranscript = request.Transcript;
            job.CorrectedTranscript = null;
            job.AccuracyScore = null;
            job.IsTranscriptApproved = false;

            await _jobRepo.UpdateAsync(job.Id, job, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating raw transcript for job {JobId}", request.JobId);
            return false;
        }
    }
}
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

public class CorrectTranscriptCommand : IRequest<CorrectTranscriptResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
}

public class CorrectTranscriptResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CorrectedTranscript { get; set; }
    public double? Accuracy { get; set; }
    public string? DetectedLanguage { get; set; }
    public List<string> Errors { get; set; } = new();

    /// <summary>True when Accuracy &lt; 90 — the creator should be prompted to revise.</summary>
    public bool NeedsRevision { get; set; }
}

public class CorrectTranscriptHandler : IRequestHandler<CorrectTranscriptCommand, CorrectTranscriptResponse>
{
    private const double AccuracyThreshold = 90.0;

    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ITextCorrectionClient _correctionClient;
    private readonly ILogger<CorrectTranscriptHandler> _logger;

    public CorrectTranscriptHandler(
        IVideoProcessingJobRepository jobRepo,
        ITextCorrectionClient correctionClient,
        ILogger<CorrectTranscriptHandler> logger)
    {
        _jobRepo = jobRepo;
        _correctionClient = correctionClient;
        _logger = logger;
    }

    public async Task<CorrectTranscriptResponse> Handle(CorrectTranscriptCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId)
                return Fail("Job not found");

            if (string.IsNullOrWhiteSpace(job.RawTranscript))
                return Fail("This job has no transcript to check yet");

            var result = await _correctionClient.CorrectAsync(job.RawTranscript, ct);
            if (!result.Success)
                return Fail(result.Error ?? "Correction failed");

            job.CorrectedTranscript = result.CorrectedText;
            job.AccuracyScore = result.Accuracy;
            job.DetectedLanguage = result.Language;
            job.IsTranscriptApproved = false; // creator must explicitly accept before continuing
            await _jobRepo.UpdateAsync(job.Id, job, ct);

            _logger.LogInformation("Transcript corrected for job {JobId}, accuracy {Accuracy}", job.Id, result.Accuracy);

            return new CorrectTranscriptResponse
            {
                Success = true,
                Message = "Transcript checked",
                CorrectedTranscript = result.CorrectedText,
                Accuracy = result.Accuracy,
                DetectedLanguage = result.Language,
                Errors = result.Errors,
                NeedsRevision = result.Accuracy < AccuracyThreshold
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error correcting transcript for job {JobId}", request.JobId);
            return Fail("An error occurred while checking the transcript");
        }
    }

    private static CorrectTranscriptResponse Fail(string msg) => new() { Success = false, Message = msg };
}
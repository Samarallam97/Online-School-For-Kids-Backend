using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Services.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

/// <summary>
/// Deletes stored video files (and marks the job as expired) for any
/// VideoProcessingJob that is older than the retention window and never
/// reached "completed" — i.e. the creator abandoned it mid-review without
/// saving any chunk as a lesson. Jobs that have at least one saved chunk are
/// left alone even past the window, since the creator may still be working
/// through the remaining chunks.
/// </summary>
public class CleanupStaleVideoJobsCommand : IRequest<int>
{
    public int RetentionDays { get; set; } = 7;
}

public class CleanupStaleVideoJobsHandler : IRequestHandler<CleanupStaleVideoJobsCommand, int>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<CleanupStaleVideoJobsHandler> _logger;

    public CleanupStaleVideoJobsHandler(
        IVideoProcessingJobRepository jobRepo,
        IFileStorageService fileStorage,
        ILogger<CleanupStaleVideoJobsHandler> logger)
    {
        _jobRepo = jobRepo;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<int> Handle(CleanupStaleVideoJobsCommand request, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-request.RetentionDays);

        var staleJobs = await _jobRepo.GetAllAsync(
            j => j.CreatedAt < cutoff
              && j.VideoUrl != null
              && j.Status != "completed",
            ct);

        var cleaned = 0;
        foreach (var job in staleJobs)
        {
            if (job.Chunks.Any(c => c.IsSaved))
                continue;

            try
            {
                await _fileStorage.DeleteFileAsync(job.VideoUrl!);
                job.VideoUrl = null;
                job.Status = "expired";
                await _jobRepo.UpdateAsync(job.Id, job, ct);
                cleaned++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up stale video for job {JobId}", job.Id);
            }
        }

        return cleaned;
    }
}
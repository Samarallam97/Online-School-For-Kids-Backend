using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;


namespace Application.Commands;

public class UpdateChunkCommand : IRequest<bool>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
}

public class UpdateChunkHandler : IRequestHandler<UpdateChunkCommand, bool>
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

    public async Task<bool> Handle(UpdateChunkCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId)
                return false;

            var chunk = job.Chunks.FirstOrDefault(c => c.Id == request.ChunkId);
            if (chunk == null) return false;

            chunk.Title = request.Title;
            chunk.Transcript = request.Transcript;

            await _jobRepo.UpdateAsync(job.Id, job, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chunk {ChunkId}", request.ChunkId);
            return false;
        }
    }
}
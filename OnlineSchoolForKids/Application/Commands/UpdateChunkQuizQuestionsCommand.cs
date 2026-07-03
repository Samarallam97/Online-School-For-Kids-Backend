using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands;

/// <summary>
/// Replaces the full question list for one difficulty on one chunk's draft
/// quiz set — used after the creator edits questions, changes an answer, or
/// adds/removes questions manually in the review UI.
/// </summary>
public class UpdateChunkQuizQuestionsCommand : IRequest<bool>
{
    public string InstructorId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<DraftQuizQuestionDto> Questions { get; set; } = new();
}

public class UpdateChunkQuizQuestionsHandler : IRequestHandler<UpdateChunkQuizQuestionsCommand, bool>
{
    private readonly IVideoProcessingJobRepository _jobRepo;
    private readonly ILogger<UpdateChunkQuizQuestionsHandler> _logger;

    public UpdateChunkQuizQuestionsHandler(
        IVideoProcessingJobRepository jobRepo,
        ILogger<UpdateChunkQuizQuestionsHandler> logger)
    {
        _jobRepo = jobRepo;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateChunkQuizQuestionsCommand request, CancellationToken ct)
    {
        try
        {
            var job = await _jobRepo.GetByIdAsync(request.JobId, ct);
            if (job == null || job.InstructorId != request.InstructorId) return false;

            var chunk = job.Chunks.FirstOrDefault(c => c.Id == request.ChunkId);
            if (chunk == null || chunk.IsSaved) return false;

            var quizSet = chunk.DraftQuizzes.FirstOrDefault(q => q.Difficulty == request.Difficulty);
            if (quizSet == null)
            {
                quizSet = new Domain.Entities.Content.DraftQuizSet { Difficulty = request.Difficulty };
                chunk.DraftQuizzes.Add(quizSet);
            }

            quizSet.Questions = request.Questions.Select(q => new Domain.Entities.Content.DraftQuizQuestion
            {
                Question = q.Question,
                Options = q.Options,
                CorrectAnswer = q.CorrectAnswer,
                Explanation = q.Explanation
            }).ToList();

            await _jobRepo.UpdateAsync(job.Id, job, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chunk quiz questions for chunk {ChunkId}", request.ChunkId);
            return false;
        }
    }
}
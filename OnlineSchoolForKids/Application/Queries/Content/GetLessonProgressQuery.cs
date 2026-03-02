using Domain.Interfaces.Repositories.Content;
using MediatR;
using static GetLessonProgressHandler;

public class GetLessonProgressQuery : IRequest<LessonProgressDetailDto?>
{
    public string UserId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
}

public class GetLessonProgressHandler : IRequestHandler<GetLessonProgressQuery, LessonProgressDetailDto?>
{
    private readonly ILessonProgressRepository _lessonProgressRepo;

    public GetLessonProgressHandler(ILessonProgressRepository lessonProgressRepo)
    {
        _lessonProgressRepo = lessonProgressRepo;
    }

    public async Task<LessonProgressDetailDto?> Handle(GetLessonProgressQuery request, CancellationToken ct)
    {
        var progress = await _lessonProgressRepo.GetOneAsync(
            lp => lp.UserId == request.UserId &&
                  lp.CourseId == request.CourseId &&
                  lp.LessonId == request.LessonId,
            ct);

        if (progress == null) return null;

        return new LessonProgressDetailDto
        {
            LessonId = progress.LessonId,
            IsCompleted = progress.IsCompleted,
            VideoPosition = progress.VideoPosition,
            TimeSpent = progress.TimeSpent,
            WatchedPercentage = progress.WatchedPercentage,
            LastAccessedAt = progress.LastAccessedAt
        };
    } 
    public class LessonProgressDetailDto
{
    public string LessonId { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int VideoPosition { get; set; } // Where to resume
    public int TimeSpent { get; set; }
    public decimal WatchedPercentage { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}
}



using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using static ToggleBookmarkHandler;

public class ToggleBookmarkCommand : IRequest<ToggleBookmarkResponse>
{
    public string UserId { get; set; } = string.Empty;
    public ToggleBookmarkDto Dto { get; set; } = new();
}

public class ToggleBookmarkHandler : IRequestHandler<ToggleBookmarkCommand, ToggleBookmarkResponse>
{
    private readonly IBookmarkRepository _bookmarkRepo;
    private readonly ILogger<ToggleBookmarkHandler> _logger;

    public ToggleBookmarkHandler(
        IBookmarkRepository bookmarkRepo,
        ILogger<ToggleBookmarkHandler> logger)
    {
        _bookmarkRepo = bookmarkRepo;
        _logger = logger;
    }

    public async Task<ToggleBookmarkResponse> Handle(
        ToggleBookmarkCommand request,
        CancellationToken ct)
    {
        try
        {
            var existing = await _bookmarkRepo.GetOneAsync(
                b => b.UserId == request.UserId &&
                     b.CourseId == request.Dto.CourseId &&
                     b.LessonId == request.Dto.LessonId,
                ct);

            if (existing != null)
            {
                // Remove
                await _bookmarkRepo.DeleteAsync(existing.Id, ct);
                return new ToggleBookmarkResponse
                {
                    Success = true,
                    IsBookmarked = false,
                    Message = "Bookmark removed"
                };
            }
            else
            {
                // Add
                var bookmark = new Bookmark
                {
                    UserId = request.UserId,
                    CourseId = request.Dto.CourseId,
                    LessonId = request.Dto.LessonId
                };
                await _bookmarkRepo.CreateAsync(bookmark, ct);
                return new ToggleBookmarkResponse
                {
                    Success = true,
                    IsBookmarked = true,
                    Message = "Bookmark added"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling bookmark");
            return new ToggleBookmarkResponse
            {
                Success = false,
                IsBookmarked = false,
                Message = "Failed to toggle bookmark"
            };
        }
    }
    public class ToggleBookmarkDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
    }

    public class ToggleBookmarkResponse
    {
        public bool Success { get; set; }
        public bool IsBookmarked { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

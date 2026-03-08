using Domain.Entities.Content.Moderation;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content.Moderation
{
    public class GetFlaggedCommentsQuery : IRequest<IEnumerable<CommentDto>>
    {
    }

    public class GetFlaggedCommentsHandler : IRequestHandler<GetFlaggedCommentsQuery, IEnumerable<CommentDto>>
    {
        private readonly ICommentRepository _commentRepo;
        private readonly ILogger<GetFlaggedCommentsHandler> _logger;

        public GetFlaggedCommentsHandler(
            ICommentRepository commentRepo,
            ILogger<GetFlaggedCommentsHandler> logger)
        {
            _commentRepo = commentRepo;
            _logger = logger;
        }

        public async Task<IEnumerable<CommentDto>> Handle(GetFlaggedCommentsQuery request, CancellationToken ct)
        {
            try
            {
                var comments = await _commentRepo.GetAllAsync(
                    c => c.IsFlagged,
                    ct);

                return comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    UserName = c.UserName,
                    Content = c.Content,
                    CourseName = c.CourseName,
                    IsFlagged = c.IsFlagged,
                    CreatedAt = c.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flagged comments");
                return Enumerable.Empty<CommentDto>();
            }
        }
    }
    public class CommentDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public bool IsFlagged { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

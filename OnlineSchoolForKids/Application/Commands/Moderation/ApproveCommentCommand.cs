using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Moderation
{
    public class ApproveCommentCommand : IRequest<bool>
    {
        public string CommentId { get; set; } = string.Empty;
    }

    public class ApproveCommentHandler : IRequestHandler<ApproveCommentCommand, bool>
    {
        private readonly ICommentRepository _commentRepo;
        private readonly ILogger<ApproveCommentHandler> _logger;

        public ApproveCommentHandler(
            ICommentRepository commentRepo,
            ILogger<ApproveCommentHandler> logger)
        {
            _commentRepo = commentRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(ApproveCommentCommand request, CancellationToken ct)
        {
            try
            {
                var comment = await _commentRepo.GetByIdAsync(request.CommentId, ct);
                if (comment == null) return false;

                comment.IsApproved = true;
                comment.IsFlagged = false;

                await _commentRepo.UpdateAsync(comment.Id, comment, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving comment");
                return false;
            }
        }
    }
    public class ApproveCommentDtoValidator : AbstractValidator<ApproveCommentDto>
    {
        public ApproveCommentDtoValidator()
        {
            RuleFor(x => x.CommentId)
                .NotEmpty().WithMessage("Comment ID is required");
        }
    }
    public class ApproveCommentDto
    {
        public string CommentId { get; set; } = string.Empty;
    }
}

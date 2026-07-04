using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;
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
        public ApproveCommentDtoValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.CommentId)
                .NotEmpty().WithMessage(localizer["CommentIdIsRequired"]);
        }
    }
    public class ApproveCommentDto
    {
        public string CommentId { get; set; } = string.Empty;
    }
}

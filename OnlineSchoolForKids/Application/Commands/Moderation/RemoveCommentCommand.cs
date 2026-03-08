using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Moderation
{
    public class RemoveCommentCommand : IRequest<bool>
    {
        public string CommentId { get; set; } = string.Empty;
    }

    public class RemoveCommentHandler : IRequestHandler<RemoveCommentCommand, bool>
    {
        private readonly ICommentRepository _commentRepo;
        private readonly ILogger<RemoveCommentHandler> _logger;

        public RemoveCommentHandler(
            ICommentRepository commentRepo,
            ILogger<RemoveCommentHandler> logger)
        {
            _commentRepo = commentRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(RemoveCommentCommand request, CancellationToken ct)
        {
            try
            {
                await _commentRepo.DeleteAsync(request.CommentId, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing comment");
                return false;
            }
        }
    }
    public class RemoveCommentDtoValidator : AbstractValidator<RemoveCommentDto>
    {
        public RemoveCommentDtoValidator()
        {
            RuleFor(x => x.CommentId)
                .NotEmpty().WithMessage("Comment ID is required");
        }
    }
    public class RemoveCommentDto
    {
        public string CommentId { get; set; } = string.Empty;
    }
}



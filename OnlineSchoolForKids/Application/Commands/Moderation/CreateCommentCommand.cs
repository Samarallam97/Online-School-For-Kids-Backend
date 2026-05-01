using Application.Queries.Content.Moderation;
using Domain.Entities.Content.Moderation;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Application.Commands.Moderation
{
    public class CreateCommentCommand : IRequest<CommentDto?>
    {
        public string UserId { get; set; } = string.Empty;
        public CreateCommentDto Dto { get; set; } = new();
    }

    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CommentDto?>
    {
        private readonly ICommentRepository _commentRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<CreateCommentHandler> _logger;

        public CreateCommentHandler(
            ICommentRepository commentRepo,
            ICourseRepository courseRepo,
            ILogger<CreateCommentHandler> logger)
        {
            _commentRepo = commentRepo;
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<CommentDto?> Handle(CreateCommentCommand request, CancellationToken ct)
        {
            try
            {
                var dto = request.Dto;

                // Get course to get course name
                var course = await _courseRepo.GetByIdAsync(dto.CourseId, ct);
                if (course == null)
                {
                    _logger.LogWarning("Course not found: {CourseId}", dto.CourseId);
                    return null;
                }

                var comment = new Comment
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    UserId = request.UserId,
                    CourseId = dto.CourseId,
                    CourseName = course.Title,
                    LessonId = dto.LessonId,
                    Content = dto.Content,
                    ParentCommentId = dto.ParentCommentId,
                    IsFlagged = false,
                    IsApproved = true // Auto-approve by default
                };

                await _commentRepo.CreateAsync(comment, ct);

                _logger.LogInformation("Comment created: {CommentId} by User {UserId} on Course {CourseId}",
                    comment.Id, request.UserId, dto.CourseId);

                return new CommentDto
                {
                    Id = comment.Id,
                    UserName = comment.UserName,
                    Content = comment.Content,
                    CourseName = comment.CourseName,
                    IsFlagged = comment.IsFlagged,
                    CreatedAt = comment.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return null;
            }
        }
    }
    public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
    {
        public CreateCommentDtoValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required")
                .MinimumLength(3).WithMessage("Comment must be at least 3 characters")
                .MaximumLength(2000).WithMessage("Comment cannot exceed 2000 characters");

            RuleFor(x => x.ParentCommentId)
                .MaximumLength(50).WithMessage("Parent comment ID cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.ParentCommentId));
        }
    }
    public class CreateCommentDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ParentCommentId { get; set; }
    }

}

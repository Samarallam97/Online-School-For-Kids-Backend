using Application.Queries.Content.Moderation;
using Domain.Entities.Content.Moderation;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Moderation
{
    public class RejectCourseCommand : IRequest<bool>
    {
        public string CourseId { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RejectCourseHandler : IRequestHandler<RejectCourseCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<RejectCourseHandler> _logger;

        public RejectCourseHandler(
            ICourseRepository courseRepo,
            ILogger<RejectCourseHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(RejectCourseCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                if (course == null) return false;

                course.IsPublished = false;
                course.ModerationStatus = new CourseModerationStatus
                {
                    Status = ModerationStatus.Rejected,
                    ReviewedAt = DateTime.UtcNow,
                    ReviewedBy = request.AdminId,
                    RejectionReason = request.Reason
                };

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Course rejected: {CourseId} by Admin {AdminId}", course.Id, request.AdminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting course");
                return false;
            }
        }
    }
    public class RejectCourseDtoValidator : AbstractValidator<RejectCourseDto>
    {
        public RejectCourseDtoValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Rejection reason is required")
                .MinimumLength(10).WithMessage("Reason must be at least 10 characters")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }
    }
}

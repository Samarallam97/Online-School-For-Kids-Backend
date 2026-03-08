using Application.Queries.Content.Moderation;
using Domain.Entities.Content.Moderation;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Moderation
{
    public class ApproveCourseCommand : IRequest<bool>
    {
        public string CourseId { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
    }

    public class ApproveCourseHandler : IRequestHandler<ApproveCourseCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<ApproveCourseHandler> _logger;

        public ApproveCourseHandler(
            ICourseRepository courseRepo,
            ILogger<ApproveCourseHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(ApproveCourseCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                if (course == null) return false;

                course.IsPublished = true;
                course.ModerationStatus = new CourseModerationStatus
                {
                    Status = ModerationStatus.Approved,
                    ReviewedAt = DateTime.UtcNow,
                    ReviewedBy = request.AdminId
                };

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Course approved: {CourseId} by Admin {AdminId}", course.Id, request.AdminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving course");
                return false;
            }
        }
    }
    public class ApproveCourseDtoValidator : AbstractValidator<ApproveCourseDto>
    {
        public ApproveCourseDtoValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");
        }
    }
}

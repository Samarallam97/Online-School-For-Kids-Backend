using Domain.Interfaces.Repositories.Content;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;
namespace Application.Commands.Profile.Creator;

public record ToggleCourseVisibilityCommand(
    string UserId,
    string CourseId,
    bool IsPublishedOnProfile
) : IRequest;

public class ToggleCourseVisibilityCommandHandler : IRequestHandler<ToggleCourseVisibilityCommand>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ToggleCourseVisibilityCommandHandler(ICourseRepository courseRepository, IStringLocalizer<SharedResource> localizer)
    {
        _courseRepository = courseRepository;
        _localizer = localizer;
    }

    public async Task Handle(ToggleCourseVisibilityCommand request, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["CourseIsNotFound"]);

        if (course.InstructorId != request.UserId)
            throw new UnauthorizedAccessException(_localizer["CourseOwnershipRequired"]);

        course.IsVisible = request.IsPublishedOnProfile;

        await _courseRepository.UpdateAsync(course.Id, course, cancellationToken);
    }
}

public class ToggleCourseVisibilityDto
{
    public bool IsPublishedOnProfile { get; set; }
}
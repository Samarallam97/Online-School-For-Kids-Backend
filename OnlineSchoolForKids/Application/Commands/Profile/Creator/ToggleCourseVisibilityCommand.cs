using Domain.Interfaces.Repositories.Content;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Creator;

public record ToggleCourseVisibilityCommand(
    string UserId,
    string CourseId,
    bool IsPublishedOnProfile
) : IRequest;

public class ToggleCourseVisibilityCommandHandler : IRequestHandler<ToggleCourseVisibilityCommand>
{
    private readonly ICourseRepository _courseRepository;

    public ToggleCourseVisibilityCommandHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task Handle(ToggleCourseVisibilityCommand request, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (course.InstructorId != request.UserId)
            throw new UnauthorizedAccessException("You do not own this course.");

        course.IsVisible = request.IsPublishedOnProfile;

        await _courseRepository.UpdateAsync(course.Id, course, cancellationToken);
    }
}

public class ToggleCourseVisibilityDto
{
    public bool IsPublishedOnProfile { get; set; }
}
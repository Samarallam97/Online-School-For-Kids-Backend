using Application.Dtos;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Creators;

public record GetCreatorCoursesQuery(string UserId) : IRequest<List<CreatorCourseDto>>;


// Assumes you have a ICourseRepository or similar — adjust to your actual course storage
public class GetCreatorCoursesQueryHandler : IRequestHandler<GetCreatorCoursesQuery, List<CreatorCourseDto>>
{
    private readonly ICourseRepository _courseRepository;

    public GetCreatorCoursesQueryHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<List<CreatorCourseDto>> Handle(GetCreatorCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.GetAllAsync(c => c.InstructorId == request.UserId, cancellationToken);

        return courses.Select(c => new CreatorCourseDto
        {
            Id = c.Id,
            Title = c.Title,
            Thumbnail = c.ThumbnailUrl,
            StudentsCount = c.EnrolledStudentIds?.Count ?? 0,
            Rating = c.Rating,
            Category = c.Category?.Name ?? string.Empty,
            IsPublishedOnProfile = c.IsVisible
        }).ToList();
    }
}

public class CreatorCourseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public int StudentsCount { get; set; }
    public decimal Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsPublishedOnProfile { get; set; }
}
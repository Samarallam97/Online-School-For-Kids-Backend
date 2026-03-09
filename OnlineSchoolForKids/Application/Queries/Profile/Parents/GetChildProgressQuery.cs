//using Application.Interfaces;
//using Application.Models;
//using MediatR;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Application.Commands.Profile;

//public class GetChildProgressQuery : IRequest<ChildProgressDto>
//{
//    public string ParentUserId { get; set; } = string.Empty;
//    public string ChildId { get; set; } = string.Empty;
//}

//public class GetChildProgressQueryHandler : IRequestHandler<GetChildProgressQuery, ChildProgressDto>
//{
//    private readonly IUserRepository _userRepository;
//    private readonly ICourseRepository _courseRepository; // You'll need to create this
//    private readonly IProgressRepository _progressRepository; // You'll need to create this

//    public GetChildProgressQueryHandler(
//        IUserRepository userRepository,
//        ICourseRepository courseRepository,
//        IProgressRepository progressRepository)
//    {
//        _userRepository = userRepository;
//        _courseRepository = courseRepository;
//        _progressRepository = progressRepository;
//    }

//    public async Task<ChildProgressDto> Handle(GetChildProgressQuery request, CancellationToken cancellationToken)
//    {
//        // Verify parent
//        var parent = await _userRepository.GetByIdAsync(request.ParentUserId);
//        if (parent == null)
//            throw new KeyNotFoundException("Parent not found");

//        if (parent.Role != Domain.Enums.UserRole.Parent)
//            throw new UnauthorizedAccessException("User is not a parent");

//        // Verify child
//        var child = await _userRepository.GetByIdAsync(request.ChildId);
//        if (child == null)
//            throw new KeyNotFoundException("Child not found");

//        // Verify this child belongs to this parent
//        if (child.ParentId != request.ParentUserId)
//            throw new UnauthorizedAccessException("This child does not belong to this parent");

//        // Get enrolled courses
//        var enrolledCourses = new List<CourseProgressDto>();

//        if (child.EnrolledCourseIds != null && child.EnrolledCourseIds.Any())
//        {
//            foreach (var courseId in child.EnrolledCourseIds)
//            {
//                var course = await _courseRepository.GetByIdAsync(courseId);
//                if (course == null) continue;

//                var progress = await _progressRepository.GetCourseProgressAsync(child.Id, courseId);

//                enrolledCourses.Add(new CourseProgressDto
//                {
//                    CourseId = course.Id,
//                    CourseTitle = course.Title,
//                    CourseThumbnail = course.ThumbnailUrl,
//                    InstructorName = course.InstructorName,
//                    ProgressPercentage = progress?.ProgressPercentage ?? 0,
//                    CompletedLessons = progress?.CompletedLessons ?? 0,
//                    TotalLessons = course.TotalLessons,
//                    EnrolledDate = progress?.EnrolledDate ?? DateTime.UtcNow,
//                    LastAccessedDate = progress?.LastAccessedDate,
//                    TimeSpentMinutes = progress?.TimeSpentMinutes ?? 0
//                });
//            }
//        }

//        // Get recent achievements
//        var achievements = await _progressRepository.GetRecentAchievementsAsync(child.Id, 5);

//        var completedCourses = enrolledCourses.Count(c => c.ProgressPercentage >= 100);
//        var averageProgress = enrolledCourses.Any()
//            ? enrolledCourses.Average(c => c.ProgressPercentage)
//            : 0;
//        var totalHours = enrolledCourses.Sum(c => c.TimeSpentMinutes) / 60;

//        return new ChildProgressDto
//        {
//            ChildId = child.Id,
//            ChildName = child.FullName,
//            EnrolledCourses = enrolledCourses,
//            TotalCoursesEnrolled = enrolledCourses.Count,
//            CompletedCourses = completedCourses,
//            AverageProgress = Math.Round(averageProgress, 2),
//            TotalHoursLearned = totalHours,
//            RecentAchievements = achievements.Select(a => new AchievementDto
//            {
//                Id = a.Id,
//                Title = a.Title,
//                Description = a.Description,
//                Icon = a.Icon,
//                EarnedAt = a.EarnedAt
//            }).ToList()
//        };
//    }
//}

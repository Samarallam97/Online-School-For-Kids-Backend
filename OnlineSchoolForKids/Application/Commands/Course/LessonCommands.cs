using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Application.Commands.Course
{
    public class CreateLessonCommand : IRequest<CreateLessonResponse>
    {
        public string InstructorId { get; set; } = string.Empty;
        public CreateLessonDto Dto { get; set; } = new();
    }

    public class CreateLessonResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? LessonId { get; set; }
    }

    public class CreateLessonHandler : IRequestHandler<CreateLessonCommand, CreateLessonResponse>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<CreateLessonHandler> _logger;

        public CreateLessonHandler(
            ICourseRepository courseRepo,
            ILogger<CreateLessonHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<CreateLessonResponse> Handle(CreateLessonCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.Dto.CourseId, ct);
                if (course == null || course.InstructorId != request.InstructorId)
                    return Fail("Course not found");

                var section = course.Sections?.FirstOrDefault(s => s.Id == request.Dto.SectionId);
                if (section == null) return Fail("Section not found");

                var lesson = new Lesson
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    CourseId = request.Dto.CourseId,
                    SectionId = request.Dto.SectionId,
                    Title = request.Dto.Title,
                    Description = request.Dto.Description,
                    Duration = request.Dto.Duration,
                    Order = request.Dto.Order,
                    VideoUrl = request.Dto.VideoUrl ?? string.Empty,
                    IsFree = request.Dto.IsFree,
                    IsPublished = false, // shell lesson — not published until content/quiz is saved
                    Materials = new List<Material>()
                };

                section.Lessons ??= new List<Lesson>();
                section.Lessons.Add(lesson);
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Lesson created: {LessonId} in Section {SectionId}", lesson.Id, section.Id);
                return new CreateLessonResponse { Success = true, Message = "Lesson created", LessonId = lesson.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lesson");
                return Fail("An error occurred while creating the lesson");
            }
        }

        private static CreateLessonResponse Fail(string msg) => new() { Success = false, Message = msg };
    }

    public class UpdateLessonCommand : IRequest<bool>
    {
        public string InstructorId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public UpdateLessonDto Dto { get; set; } = new();
    }

    public class UpdateLessonHandler : IRequestHandler<UpdateLessonCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<UpdateLessonHandler> _logger;

        public UpdateLessonHandler(
            ICourseRepository courseRepo,
            ILogger<UpdateLessonHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateLessonCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                if (course == null || course.InstructorId != request.InstructorId) return false;

                var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
                if (section == null) return false;

                var lesson = section.Lessons?.FirstOrDefault(l => l.Id == request.LessonId);
                if (lesson == null) return false;

                lesson.Title = request.Dto.Title;
                lesson.Description = request.Dto.Description;
                lesson.Duration = request.Dto.Duration;
                lesson.Order = request.Dto.Order;
                lesson.VideoUrl = request.Dto.VideoUrl ?? lesson.VideoUrl;
                lesson.IsFree = request.Dto.IsFree;
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Lesson updated: {LessonId}", lesson.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson");
                return false;
            }
        }
    }

    public class DeleteLessonCommand : IRequest<bool>
    {
        public string InstructorId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
    }

    public class DeleteLessonHandler : IRequestHandler<DeleteLessonCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<DeleteLessonHandler> _logger;

        public DeleteLessonHandler(
            ICourseRepository courseRepo,
            ILogger<DeleteLessonHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteLessonCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                if (course == null || course.InstructorId != request.InstructorId) return false;

                var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
                if (section == null) return false;

                var lesson = section.Lessons?.FirstOrDefault(l => l.Id == request.LessonId);
                if (lesson == null) return false;

                section.Lessons!.Remove(lesson);
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Lesson deleted: {LessonId}", lesson.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lesson");
                return false;
            }
        }
    }

    public class CreateLessonDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration { get; set; } // Seconds
        public int Order { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsFree { get; set; } = false;
    }

    public class UpdateLessonDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public int Order { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsFree { get; set; }
    }
}
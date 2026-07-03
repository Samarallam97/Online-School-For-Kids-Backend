using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Application.Commands.Course
{
    public class CreateSectionCommand : IRequest<bool>
    {
        public CreateSectionDto Dto { get; set; } = new();
        public string InstructorId { get; set; } = string.Empty;
    }

    public class CreateSectionHandler : IRequestHandler<CreateSectionCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<CreateSectionHandler> _logger;

        public CreateSectionHandler(
            ICourseRepository courseRepo,
            ILogger<CreateSectionHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(CreateSectionCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.Dto.CourseId, ct);
                if (course == null || course.InstructorId != request.InstructorId) return false;

                course.Sections ??= new List<Section>();

                var section = new Section
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Title = request.Dto.Title,
                    Description = request.Dto.Description,
                    Order = request.Dto.Order,
                    Lessons = new List<Lesson>(),
                    CourseId = course.Id
                };

                course.Sections.Add(section);
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);
                _logger.LogInformation("Section created: {SectionId} in Course {CourseId}", section.Id, course.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating section");
                return false;
            }
        }

        public class UpdateSectionCommand : IRequest<bool>
        {
            public string CourseId { get; set; } = string.Empty;
            public string SectionId { get; set; } = string.Empty;
            public string InstructorId { get; set; } = string.Empty;
            public UpdateSectionDto Dto { get; set; } = new();
        }

        public class UpdateSectionHandler : IRequestHandler<UpdateSectionCommand, bool>
        {
            private readonly ICourseRepository _courseRepo;
            private readonly ILogger<UpdateSectionHandler> _logger;

            public UpdateSectionHandler(
                ICourseRepository courseRepo,
                ILogger<UpdateSectionHandler> logger)
            {
                _courseRepo = courseRepo;
                _logger = logger;
            }

            public async Task<bool> Handle(UpdateSectionCommand request, CancellationToken ct)
            {
                try
                {
                    var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                    if (course == null || course.InstructorId != request.InstructorId) return false;

                    var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
                    if (section == null) return false;

                    section.Title = request.Dto.Title;
                    section.Description = request.Dto.Description;
                    section.Order = request.Dto.Order;
                    course.UpdatedAt = DateTime.UtcNow;

                    await _courseRepo.UpdateAsync(course.Id, course, ct);

                    _logger.LogInformation("Section updated: {SectionId}", section.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating section");
                    return false;
                }
            }
        }

        public class DeleteSectionCommand : IRequest<bool>
        {
            public string CourseId { get; set; } = string.Empty;
            public string SectionId { get; set; } = string.Empty;
            public string InstructorId { get; set; } = string.Empty;
        }

        public class DeleteSectionHandler : IRequestHandler<DeleteSectionCommand, bool>
        {
            private readonly ICourseRepository _courseRepo;
            private readonly ILogger<DeleteSectionHandler> _logger;

            public DeleteSectionHandler(
                ICourseRepository courseRepo,
                ILogger<DeleteSectionHandler> logger)
            {
                _courseRepo = courseRepo;
                _logger = logger;
            }

            public async Task<bool> Handle(DeleteSectionCommand request, CancellationToken ct)
            {
                try
                {
                    var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                    if (course == null || course.InstructorId != request.InstructorId) return false;

                    var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
                    if (section == null) return false;

                    course.Sections.Remove(section);
                    course.UpdatedAt = DateTime.UtcNow;

                    await _courseRepo.UpdateAsync(course.Id, course, ct);

                    _logger.LogInformation("Section deleted: {SectionId}", section.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting section");
                    return false;
                }
            }
        }

    }
    public class CreateSectionDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; } = 1;
    }

    public class UpdateSectionDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }
    }
}
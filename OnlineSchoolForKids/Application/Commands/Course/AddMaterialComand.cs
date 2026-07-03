using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Application.Commands.Course
{
    // ── Add ──────────────────────────────────────────────────────────────

    public class AddMaterialCommand : IRequest<AddMaterialResponse>
    {
        public string InstructorId { get; set; } = string.Empty;
        public AddMaterialDto Dto { get; set; } = new();
    }

    public class AddMaterialResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MaterialId { get; set; }
    }

    public class AddMaterialHandler : IRequestHandler<AddMaterialCommand, AddMaterialResponse>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<AddMaterialHandler> _logger;

        public AddMaterialHandler(
            ICourseRepository courseRepo,
            ILogger<AddMaterialHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<AddMaterialResponse> Handle(AddMaterialCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.Dto.CourseId, ct);
                if (course == null || course.InstructorId != request.InstructorId)
                    return Fail("Course not found");

                var section = course.Sections?.FirstOrDefault(s => s.Id == request.Dto.SectionId);
                if (section == null) return Fail("Section not found");

                var lesson = section.Lessons?.FirstOrDefault(l => l.Id == request.Dto.LessonId);
                if (lesson == null) return Fail("Lesson not found");

                var material = new Material
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Title = request.Dto.Title,
                    Type = request.Dto.Type,
                    Url = request.Dto.Url,
                    FileSize = request.Dto.FileSize,
                    LessonId = lesson.Id
                };

                lesson.Materials ??= new List<Material>();
                lesson.Materials.Add(material);
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Material added: {MaterialId} to Lesson {LessonId}", material.Id, lesson.Id);
                return new AddMaterialResponse { Success = true, Message = "Material added", MaterialId = material.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding material");
                return Fail("An error occurred while adding the material");
            }
        }

        private static AddMaterialResponse Fail(string msg) => new() { Success = false, Message = msg };
    }

    // ── Update ───────────────────────────────────────────────────────────

    public class UpdateMaterialCommand : IRequest<bool>
    {
        public string InstructorId { get; set; } = string.Empty;
        public UpdateMaterialDto Dto { get; set; } = new();
    }

    public class UpdateMaterialHandler : IRequestHandler<UpdateMaterialCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<UpdateMaterialHandler> _logger;

        public UpdateMaterialHandler(
            ICourseRepository courseRepo,
            ILogger<UpdateMaterialHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateMaterialCommand request, CancellationToken ct)
        {
            try
            {
                var dto = request.Dto;
                var course = await _courseRepo.GetByIdAsync(dto.CourseId, ct);
                if (course == null || course.InstructorId != request.InstructorId) return false;

                var section = course.Sections?.FirstOrDefault(s => s.Id == dto.SectionId);
                var lesson = section?.Lessons?.FirstOrDefault(l => l.Id == dto.LessonId);
                var material = lesson?.Materials?.FirstOrDefault(m => m.Id == dto.MaterialId);
                if (material == null) return false;

                material.Title = dto.Title;
                material.Type = dto.Type;
                material.Url = dto.Url ?? material.Url;
                material.FileSize = dto.FileSize ?? material.FileSize;
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Material updated: {MaterialId}", material.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating material {MaterialId}", request.Dto.MaterialId);
                return false;
            }
        }
    }

    // ── Delete ───────────────────────────────────────────────────────────

    public class DeleteMaterialCommand : IRequest<bool>
    {
        public string InstructorId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
    }

    public class DeleteMaterialHandler : IRequestHandler<DeleteMaterialCommand, bool>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly ILogger<DeleteMaterialHandler> _logger;

        public DeleteMaterialHandler(
            ICourseRepository courseRepo,
            ILogger<DeleteMaterialHandler> logger)
        {
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteMaterialCommand request, CancellationToken ct)
        {
            try
            {
                var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
                if (course == null || course.InstructorId != request.InstructorId) return false;

                var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
                var lesson = section?.Lessons?.FirstOrDefault(l => l.Id == request.LessonId);
                var material = lesson?.Materials?.FirstOrDefault(m => m.Id == request.MaterialId);
                if (material == null) return false;

                lesson!.Materials.Remove(material);
                course.UpdatedAt = DateTime.UtcNow;

                await _courseRepo.UpdateAsync(course.Id, course, ct);

                _logger.LogInformation("Material deleted: {MaterialId}", request.MaterialId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting material {MaterialId}", request.MaterialId);
                return false;
            }
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────

    public class AddMaterialDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class UpdateMaterialDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Url { get; set; }
        public long? FileSize { get; set; }
    }
}
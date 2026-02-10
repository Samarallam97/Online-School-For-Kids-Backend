using Application.Commands;
using Application.Dtos;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using CourseEntity = Domain.Entities.Course;

namespace EduPlatform.Application.Commands.CreateCourse
{
    public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDto>
    {
        private readonly IGenericRepository<CourseEntity> _courseRepo;

        public CreateCourseCommandHandler( IGenericRepository<CourseEntity> courseRepo)
        {
            _courseRepo = courseRepo;
        }

        public async Task<CourseDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
        {
            //var instructor = await _unitOfWork.Instructors.FindAsync(i => i.UserId == request.InstructorId);

            //if (!instructor.Any())
            //    throw new UnauthorizedAccessException("User is not an instructor");

            var course = new Course
            {
                Id = Guid.NewGuid().ToString(),
                Title = request.Title,
                Description = request.Description,
                //InstructorId = request.CreatorId,
                CategoryId = request.CategoryId,
                Level = request.Level,
                Price = request.Price,
                DiscountPrice = request.DiscountPrice,
                DurationHours = request.DurationHours,
                ThumbnailUrl = request.ThumbnailUrl,
                Language = request.Language,
                Rating = 0,
                TotalStudents = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPublished = false,
                IsFeatured = false
            };

            await _courseRepo.CreateAsync(course);


            return MapToCourseDto(course);
        }

        private CourseDto MapToCourseDto(Course course)
        {
            return new CourseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                //InstructorName = course.Instructor?.User?.FullName ?? "Unknown",
                InstructorId = course.InstructorId,
                CategoryName = course.Category?.Name ?? "Unknown",
                CategoryId = course.CategoryId,
                Level = course.Level,
                LevelDisplay = course.Level.ToString(),
                Price = course.Price,
                DiscountPrice = course.DiscountPrice,
                Rating = course.Rating,
                TotalStudents = course.TotalStudents,
                DurationHours = course.DurationHours,
                ThumbnailUrl = course.ThumbnailUrl,
                Language = course.Language,
                IsFeatured = course.IsFeatured,
                IsInWishlist = false,
                IsInCart = false

            };
        }


    }
    public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, UpdateCourseResponse>
    {

        private readonly IMapper _mapper;
        private readonly IGenericRepository<CourseEntity> _courseRepo;

        public UpdateCourseCommandHandler(
            
            IMapper mapper
           , IGenericRepository<CourseEntity> courseRepo)
        {
     
            _mapper = mapper;
            _courseRepo = courseRepo;
        }
        public async Task<UpdateCourseResponse> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
        {

            // Get the course
            var course = await _courseRepo.GetByIdAsync(request.CourseId);
            if (course == null || course.IsDeleted || course.InstructorId != request.CreatorId)
            {
                throw new InvalidOperationException("Course not found or access denied.");
            }
            // Validate discount price
            if (request.DiscountPrice.HasValue && request.DiscountPrice.Value > request.Price)
            {
                return new UpdateCourseResponse
                {
                    Success = false,
                    Message = "Discount price cannot be greater than regular price"
                };
            }

            // Update course properties
            course.Title = request.Title;
            course.Description = request.Description;
            course.CategoryId = request.CategoryId;
            course.Level = request.Level;
            course.Price = request.Price;
            course.DiscountPrice = request.DiscountPrice;
            course.DurationHours = request.DurationHours;
            course.Language = request.Language;
            course.IsPublished = request.IsPublished;
            course.IsFeatured = request.IsFeatured;
            course.UpdatedAt = DateTime.UtcNow;

            // Only update thumbnail if provided
            if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
            {
                course.ThumbnailUrl = request.ThumbnailUrl;
            }

            // Save changes
            await _courseRepo.UpdateAsync(request.CourseId, course);
            //await _unitOfWork.SaveChangesAsync();

            // Map to DTO
            var courseDto = _mapper.Map<CourseDto>(course);

            return new UpdateCourseResponse
            {
                Success = true,
                Message = "Course updated successfully",
                Course = courseDto
            };
        }
    }
}

















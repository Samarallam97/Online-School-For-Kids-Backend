using Application.Dtos;
using MediatR;

namespace Application.Queries
{
    public class GetCourseByIdQuery : IRequest<CourseDto>
    {
        public string CourseId { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }
}

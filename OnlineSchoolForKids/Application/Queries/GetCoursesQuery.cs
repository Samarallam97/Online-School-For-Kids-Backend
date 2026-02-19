using Application.Dtos;
using Domain.Enums.Content;
using MediatR;


namespace Application.Queries
{
    public class GetCoursesQuery : IRequest<PagedResult<CourseDto>>
    {
        public string? CategoryId { get; set; }
        public CourseLevel? Level { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinRating { get; set; }
        public string? Language { get; set; }
        public string? SearchQuery { get; set; }
        public string SortBy { get; set; } = "relevance";
        public string SortOrder { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        public string? UserId { get; set; }
    }
}
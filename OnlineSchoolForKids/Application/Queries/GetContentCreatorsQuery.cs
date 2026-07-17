using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Domain.Interfaces.Repositories.Users;
using MediatR;

namespace Application.Queries;



public record GetContentCreatorsQuery : IRequest<PagedResult<ContentCreatorListItemDto>>
{
    public string? Search { get; init; }
    public string? ExpertiseTag { get; init; }
    public string SortBy { get; init; } = "rating";
    public string SortOrder { get; init; } = "desc";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}

public class ContentCreatorListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public string? Country { get; set; }
    public List<string> ExpertiseTags { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    public int StudentsCount { get; set; }
    public int CoursesCount { get; set; }
    public bool IsVerifiedCreator { get; set; }
    public string? PortfolioUrl { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class GetContentCreatorsQueryHandler
    : IRequestHandler<GetContentCreatorsQuery, PagedResult<ContentCreatorListItemDto>>
{
    private readonly IUserRepository _userRepository;

    public GetContentCreatorsQueryHandler(IUserRepository userRepository)
        => _userRepository = userRepository;

    public async Task<PagedResult<ContentCreatorListItemDto>> Handle(
        GetContentCreatorsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var skip = (page - 1) * pageSize;

        var (items, totalCount) = await _userRepository.GetContentCreatorsPagedAsync(
            request.Search,
            request.ExpertiseTag,
            request.SortBy,
            request.SortOrder,
            skip,
            pageSize,
            ct);

        var dtos = items.Select(u => new ContentCreatorListItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            ProfilePictureUrl = u.ProfilePictureUrl,
            Bio = u.Bio,
            Country = u.Country,
            ExpertiseTags = u.ExpertiseTags ?? new List<string>(),
            AverageRating = u.AverageRating ?? 0,
            ReviewsCount = u.ReviewsCount ?? 0,
            StudentsCount = u.TotalStudents ?? 0,
            CoursesCount = u.CoursesCount ?? 0,
            IsVerifiedCreator = u.IsVerifiedCreator ?? false,
            PortfolioUrl = u.PortfolioUrl,
        }).ToList();

        return new PagedResult<ContentCreatorListItemDto>
        {
            Items = dtos,
            TotalCount = (int)totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Page = page,
            PageSize = pageSize,
        };
    }
}
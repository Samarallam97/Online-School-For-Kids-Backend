using Domain.Interfaces.Repositories.Content;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Content;

public sealed record GetCategoriesQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<GetCategoriesResult>;

public sealed class GetCategoriesHandler
    : IRequestHandler<GetCategoriesQuery, GetCategoriesResult>
{
    private readonly ICategoryRepository _repo;

    public GetCategoriesHandler(ICategoryRepository repo) => _repo = repo;

    public async Task<GetCategoriesResult> Handle(
        GetCategoriesQuery query,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var all = await _repo.GetAllAsync(cancellationToken: ct);

        // ── 1. Filter (partial match, case-insensitive) ───────────────────────
        var filtered = all.OrderBy(c => c.DisplayOrder).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.Name.ToLowerInvariant().Contains(term) ||
                (c.Description ?? "").ToLowerInvariant().Contains(term));
        }

        // ── 2. Paginate on the filtered set ──────────────────────────────────
        var totalCount = filtered.Count();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(query.Page, 1, totalPages);

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryItemDto(
                c.Id,
                c.Name,
                c.DisplayOrder,
                c.Description ?? string.Empty,
                c.ImageUrl    ?? string.Empty,
                c.CoursesCount))
            .ToList();

        return new GetCategoriesResult(
            Items: items,
            TotalCount: totalCount,
            TotalPages: totalPages,
            Page: page,
            PageSize: pageSize,
            HasNextPage: page < totalPages,
            HasPreviousPage: page > 1);
    }
}

public sealed record GetCategoriesResult(
    IReadOnlyList<CategoryItemDto> Items,
    int TotalCount,
    int TotalPages,
    int Page,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage
);

public sealed record CategoryItemDto(
    string Id,
    string Name,
    int DisplayOrder,
    string Description,
    string ImageUrl,
    int? CoursesCount
);

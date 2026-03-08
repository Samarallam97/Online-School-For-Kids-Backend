using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Admin;

public record GetUsersQuery(
    string? Search,
    string? Role,
    string? Status,
    int Page,
    int Limit,
    bool ExcludeAdmins   // true for non-super-admin callers
) : IRequest<GetUsersResponse>;

// ── Handler ────────────────────────────────────────────────────────────────

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, GetUsersResponse>
{
    private readonly IUserRepository _userRepo;
    public GetUsersQueryHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<GetUsersResponse> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 100);
        var skip = (page - 1) * limit;

        var (items, totalCount) = await _userRepo.GetUsersPagedAsync(
            request.Search,
            request.Role,
            request.Status,
            request.ExcludeAdmins,
            skip,
            limit,
            ct);

        var users = items.Select(MapToDto).ToList();

        return new GetUsersResponse
        {
            Users      = users,
            Total      = totalCount,
            Page       = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)limit),
        };
    }

    private static AdminUserDto MapToDto(User u) => new()
    {
        Id         = u.Id,
        Name       = u.FullName,
        Email      = u.Email,
        Role       = u.Role.ToString(),
        Status     = u.Status.ToString().ToLower(),
        JoinedDate = u.CreatedAt,
    };
}















public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; }
}

    public class GetUsersResponse
{
    public List<AdminUserDto> Users { get; set; } = new();
    public long Total { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
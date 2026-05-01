namespace Application.Queries.Profile.Admin;

using Domain.Interfaces.Repositories.Users;
using MediatR;

public record GetActivityLogQuery(string UserId, int Page, int Limit) : IRequest<AdminActivityLogPagedDto>;

public class GetActivityLogQueryHandler : IRequestHandler<GetActivityLogQuery, AdminActivityLogPagedDto>
{
    private readonly IUserRepository _userRepository;

    public GetActivityLogQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AdminActivityLogPagedDto> Handle(GetActivityLogQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var log = user.ActivityLog ?? [];

        // Sort newest first, then page
        var sorted = log
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var total = sorted.Count;

        var paged = sorted
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(e => new AdminActivityLogDto
            {
                Id = e.Id,
                Action = e.Action,
                Target = e.Details,       // Details maps to target display text
                TargetType = null,        // Extend ActivityLogEntry if needed
                PerformedAt = e.Timestamp,
                IpAddress = null          // Extend ActivityLogEntry if needed
            })
            .ToList();

        return new AdminActivityLogPagedDto
        {
            Activities = paged,
            Total = total
        };
    }
}

public class AdminActivityLogDto
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? IpAddress { get; set; }
}

public class AdminActivityLogPagedDto
{
    public List<AdminActivityLogDto> Activities { get; set; } = [];
    public long Total { get; set; }
}
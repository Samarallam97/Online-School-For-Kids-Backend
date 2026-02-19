using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Specialists;

using Application.DTOs.Profile;
using Domain.Interfaces.Repositories.Users;
using MediatR;
public record GetAvailabilityQuery(string UserId) : IRequest<List<AvailabilitySlotDto>>;

public class GetAvailabilityQueryHandler : IRequestHandler<GetAvailabilityQuery, List<AvailabilitySlotDto>>
{
    private readonly IUserRepository _userRepository;

    public GetAvailabilityQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<AvailabilitySlotDto>> Handle(GetAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return (user.Availability ?? []).Select(s => new AvailabilitySlotDto
        {
            Id = s.Id,
            Day = s.Day,
            StartTime = s.StartTime,
            EndTime = s.EndTime
        }).ToList();
    }
}

public class AvailabilitySlotDto
{
    public string Id { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
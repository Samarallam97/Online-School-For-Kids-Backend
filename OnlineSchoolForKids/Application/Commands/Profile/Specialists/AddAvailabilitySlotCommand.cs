using Application.Queries.Profile.Specialists;
using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Specialists;

public record AddAvailabilitySlotCommand(
    string UserId,
    string Day,
    string StartTime,
    string EndTime
) : IRequest<AvailabilitySlotDto>;


public class AddAvailabilitySlotCommandHandler : IRequestHandler<AddAvailabilitySlotCommand, AvailabilitySlotDto>
{
    private readonly IUserRepository _userRepository;

    public AddAvailabilitySlotCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AvailabilitySlotDto> Handle(AddAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid().ToString(),
            Day = request.Day,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        user.Availability ??= [];
        user.Availability.Add(slot);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        return new AvailabilitySlotDto
        {
            Id = slot.Id,
            Day = slot.Day,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime
        };
    }
}

public class AddAvailabilitySlotDto
{
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
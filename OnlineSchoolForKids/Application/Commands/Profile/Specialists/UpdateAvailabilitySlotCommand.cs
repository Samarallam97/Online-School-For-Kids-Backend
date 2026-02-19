using Application.Queries.Profile.Specialists;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Specialists;

public record UpdateAvailabilitySlotCommand(
    string UserId,
    string SlotId,
    string Day,
    string StartTime,
    string EndTime
) : IRequest<AvailabilitySlotDto>;

public class UpdateAvailabilitySlotCommandHandler : IRequestHandler<UpdateAvailabilitySlotCommand, AvailabilitySlotDto>
{
    private readonly IUserRepository _userRepository;

    public UpdateAvailabilitySlotCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AvailabilitySlotDto> Handle(UpdateAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var slot = user.Availability?.FirstOrDefault(s => s.Id == request.SlotId)
            ?? throw new KeyNotFoundException("Availability slot not found.");

        slot.Day = request.Day;
        slot.StartTime = request.StartTime;
        slot.EndTime = request.EndTime;

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
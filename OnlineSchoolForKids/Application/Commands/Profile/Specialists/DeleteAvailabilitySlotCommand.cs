using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Specialists;

using Domain.Interfaces.Repositories.Users;
using MediatR;


public record DeleteAvailabilitySlotCommand(string UserId, string SlotId) : IRequest;

public class DeleteAvailabilitySlotCommandHandler : IRequestHandler<DeleteAvailabilitySlotCommand>
{
    private readonly IUserRepository _userRepository;

    public DeleteAvailabilitySlotCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var slot = user.Availability?.FirstOrDefault(s => s.Id == request.SlotId)
            ?? throw new KeyNotFoundException("Availability slot not found.");

        user.Availability!.Remove(slot);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
    }
}
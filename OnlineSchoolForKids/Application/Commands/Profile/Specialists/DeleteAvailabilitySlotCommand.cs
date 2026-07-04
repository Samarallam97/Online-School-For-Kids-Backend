namespace Application.Commands.Profile.Specialists;

using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

public record DeleteAvailabilitySlotCommand(string UserId, string SlotId) : IRequest;

public class DeleteAvailabilitySlotCommandHandler : IRequestHandler<DeleteAvailabilitySlotCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteAvailabilitySlotCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task Handle(DeleteAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["UserNotFound"]);

        var slot = user.Availability?.FirstOrDefault(s => s.Id == request.SlotId)
            ?? throw new KeyNotFoundException(_localizer["AvailabilitySlotNotFound"]);

        user.Availability!.Remove(slot);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
    }
}
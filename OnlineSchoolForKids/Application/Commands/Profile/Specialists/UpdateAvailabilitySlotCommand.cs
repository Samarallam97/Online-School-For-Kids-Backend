using Application.Queries.Profile.Specialists;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateAvailabilitySlotCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<AvailabilitySlotDto> Handle(UpdateAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["UserNotFound"]);

        var slot = user.Availability?.FirstOrDefault(s => s.Id == request.SlotId)
            ?? throw new KeyNotFoundException(_localizer["AvailabilitySlotNotFound"]);

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
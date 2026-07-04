using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Profile.Users;

public class UpdateNotificationPreferencesCommand : IRequest<NotificationPreferences>
{
    public string UserId { get; set; } = string.Empty;
    public NotificationPreferences Preferences { get; set; } = new();
}

public class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferences>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateNotificationPreferencesCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<NotificationPreferences> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);

        user.NotificationPreferences = request.Preferences;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);

        return request.Preferences;
    }
}


using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Users;

public class UpdateNotificationPreferencesCommand : IRequest<NotificationPreferences>
{
    public string UserId { get; set; } = string.Empty;
    public NotificationPreferences Preferences { get; set; } = new();
}

public class UpdateNotificationPreferencesCommandHandler : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferences>
{
    private readonly IUserRepository _userRepository;

    public UpdateNotificationPreferencesCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<NotificationPreferences> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("user not found");

        user.NotificationPreferences = request.Preferences;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);

        return request.Preferences;
    }
}


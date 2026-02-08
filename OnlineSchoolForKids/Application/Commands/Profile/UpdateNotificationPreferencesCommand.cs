using Application.Interfaces;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

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
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        parent.NotificationPreferences = request.Preferences;
        parent.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(parent.Id, parent);

        return request.Preferences;
    }
}


using Application.Interfaces;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

public class GetNotificationPreferencesQuery : IRequest<NotificationPreferences>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferences>
{
    private readonly IUserRepository _userRepository;

    public GetNotificationPreferencesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<NotificationPreferences> Handle(GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        return parent.NotificationPreferences ?? new NotificationPreferences
        {
            ProgressUpdates = true,
            WeeklyReports = true,
            AchievementAlerts = true,
            PaymentReminders = true
        };
    }
}
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Users;

public class GetNotificationPreferencesCommand : IRequest<NotificationPreferences>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetNotificationPreferencesCommandHandler : IRequestHandler<GetNotificationPreferencesCommand, NotificationPreferences>
{
    private readonly IUserRepository _userRepository;

    public GetNotificationPreferencesCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<NotificationPreferences> Handle(GetNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Return existing preferences or defaults based on role
        if (user.NotificationPreferences != null)
            return user.NotificationPreferences;

        // Default preferences based on role
        return GetDefaultPreferencesByRole(user.Role);
    }

    private NotificationPreferences GetDefaultPreferencesByRole(UserRole role)
    {
        var preferences = new NotificationPreferences();

        if (role == UserRole.Student || role == UserRole.Parent)
        {
            preferences.ProgressUpdates = true;
            preferences.WeeklyReports = true;
            preferences.AchievementAlerts = true;
            preferences.PaymentReminders = true;
        }
        else if (role == UserRole.ContentCreator )
        {
            preferences.CourseEnrollments = false;
            preferences.ProgressUpdates = true;
            preferences.WeeklyReports = true;
            preferences.AchievementAlerts = true;
            preferences.ReviewNotifications = false;
            preferences.StudentMessages = false;
            preferences.PayoutAlerts = false;
        }

        return preferences;
    }
}
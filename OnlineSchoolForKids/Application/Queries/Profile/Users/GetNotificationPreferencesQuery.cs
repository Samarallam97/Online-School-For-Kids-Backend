using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;


namespace Application.Queries.Profile.Users;

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
            preferences.Messages = true;
            preferences.WeeklyReports = true;
            preferences.CommentReplies = true;

            preferences.ProgressUpdates = true;
            preferences.AchievementAlerts = true;
            preferences.PaymentReminders = true;
        }
        else if (role == UserRole.ContentCreator )
        {
            preferences.Messages = true;
            preferences.WeeklyReports = true;
            preferences.CommentReplies = true;

            preferences.CourseEnrollments = true;
            preferences.ReviewNotifications = true;
            preferences.PayoutAlerts = true;
        }
        else if (role == UserRole.Specialist)
        {
            preferences.Messages = true;
            preferences.WeeklyReports = true;
            preferences.CommentReplies = true;

            preferences.NewSessionBooking = true;
            preferences.SessionCancellation = true;
            preferences.SessionReminder = true;
        }
        else if (role == UserRole.Admin)
        {
            preferences.AccountLogin = true;
            preferences.SuspiciousActivity = true;
        }

        return preferences;
    }
}
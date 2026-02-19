using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Parents;

public class GetChildNotificationPreferencesCommand : IRequest<NotificationPreferences>
{
    public string ParentUserId { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
}

public class GetChildNotificationPreferencesCommandHandler
    : IRequestHandler<GetChildNotificationPreferencesCommand, NotificationPreferences>
{
    private readonly IUserRepository _userRepository;

    public GetChildNotificationPreferencesCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<NotificationPreferences> Handle(
        GetChildNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        // Verify parent
        var parent = await _userRepository.GetByIdAsync(request.ParentUserId, cancellationToken);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        // Verify child
        var child = await _userRepository.GetByIdAsync(request.ChildId, cancellationToken);
        if (child == null)
            throw new KeyNotFoundException("Child not found");

        if (child.Role != UserRole.Student)
            throw new InvalidOperationException("User is not a student");

        // Verify child is linked to this parent
        if (child.ParentId != request.ParentUserId)
            throw new UnauthorizedAccessException("This child is not linked to your account");

        // Get notification preferences for this child from parent's settings
        if (parent.ChildNotificationPreferences != null &&
            parent.ChildNotificationPreferences.TryGetValue(request.ChildId, out var preferences))
        {
            return preferences;
        }

        // Return default preferences if none exist
        return new NotificationPreferences
        {
            ProgressUpdates = true,
            WeeklyReports = true,
            AchievementAlerts = true,
            PaymentReminders = true
        };
    }
}

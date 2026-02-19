using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Parents;

public class UpdateChildNotificationPreferencesCommand : IRequest<NotificationPreferences>
{
    public string ParentUserId { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
    public NotificationPreferences Preferences { get; set; } = new();
}

public class UpdateChildNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateChildNotificationPreferencesCommand, NotificationPreferences>
{
    private readonly IUserRepository _userRepository;

    public UpdateChildNotificationPreferencesCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<NotificationPreferences> Handle(
        UpdateChildNotificationPreferencesCommand request,
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

        // Initialize ChildNotificationPreferences dictionary if it doesn't exist
        if (parent.ChildNotificationPreferences == null)
        {
            parent.ChildNotificationPreferences = new Dictionary<string, NotificationPreferences>();
        }

        // Update or add notification preferences for this child
        parent.ChildNotificationPreferences[request.ChildId] = request.Preferences;
        parent.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(parent.Id, parent, cancellationToken);

        return request.Preferences;
    }
}
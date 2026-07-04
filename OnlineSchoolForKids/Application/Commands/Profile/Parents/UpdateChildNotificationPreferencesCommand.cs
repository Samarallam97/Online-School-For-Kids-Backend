using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateChildNotificationPreferencesCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<NotificationPreferences> Handle(
        UpdateChildNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        // Verify parent
        var parent = await _userRepository.GetByIdAsync(request.ParentUserId, cancellationToken);
        if (parent == null)
            throw new KeyNotFoundException(_localizer["ParentNotFound"]);

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException(_localizer["UserIsNotParent"]);

        // Verify child
        var child = await _userRepository.GetByIdAsync(request.ChildId, cancellationToken);
        if (child == null)
            throw new KeyNotFoundException(_localizer["ChildNotFound"]);

        if (child.Role != UserRole.Student)
            throw new InvalidOperationException(_localizer["UserIsNotStudent"]);

        // Verify child is linked to this parent
        if (child.ParentId != request.ParentUserId)
            throw new UnauthorizedAccessException(_localizer["ChildNotLinkedToAccount"]);

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
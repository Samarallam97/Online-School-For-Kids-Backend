using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Parents;

public class RemoveChildCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
}

public class RemoveChildCommandHandler : IRequestHandler<RemoveChildCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RemoveChildCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(RemoveChildCommand request, CancellationToken cancellationToken)
    {
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException(_localizer["ParentNotFound"]);

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException(_localizer["UserIsNotParent"]);

        var child = await _userRepository.GetByIdAsync(request.ChildId);
        if (child == null)
            throw new KeyNotFoundException(_localizer["ChildNotFound"]);

        if (child.ParentId != request.UserId)
            throw new UnauthorizedAccessException(_localizer["ChildDoesNotBelongToParent"]);

        // Remove child from parent's list
        if (parent.ChildrenIds != null && parent.ChildrenIds.Contains(request.ChildId))
        {
            parent.ChildrenIds.Remove(request.ChildId);
            parent.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(parent.Id, parent);
        }

        // Remove parent reference from child
        child.ParentId = null;
        child.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(child.Id, child);

        return Unit.Value;
    }
}
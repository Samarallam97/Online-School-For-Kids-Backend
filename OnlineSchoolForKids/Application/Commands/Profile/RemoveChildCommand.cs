using Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

public class RemoveChildCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
}

public class RemoveChildCommandHandler : IRequestHandler<RemoveChildCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public RemoveChildCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(RemoveChildCommand request, CancellationToken cancellationToken)
    {
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        var child = await _userRepository.GetByIdAsync(request.ChildId);
        if (child == null)
            throw new KeyNotFoundException("Child not found");

        if (child.ParentId != request.UserId)
            throw new UnauthorizedAccessException("This child does not belong to this parent");

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
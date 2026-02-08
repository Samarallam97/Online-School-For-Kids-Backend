using Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

public class SendChildInviteCommand : IRequest<Unit>
{
    public string ParentUserId { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
}

public class SendChildInviteCommandHandler : IRequestHandler<SendChildInviteCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService; // You'll need to implement this

    public SendChildInviteCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<Unit> Handle(SendChildInviteCommand request, CancellationToken cancellationToken)
    {
        // Verify parent
        var parent = await _userRepository.GetByIdAsync(request.ParentUserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        // Verify child
        var child = await _userRepository.GetByIdAsync(request.ChildId);
        if (child == null)
            throw new KeyNotFoundException("Child not found");

        if (child.Role != Domain.Enums.UserRole.Student)
            throw new InvalidOperationException("User is not a student");

        // Check if already linked to another parent
        if (!string.IsNullOrEmpty(child.ParentId) && child.ParentId != request.ParentUserId)
            throw new InvalidOperationException("This child is already linked to another parent");

        // Create invitation token (you might want to store this in a separate table)
        var inviteToken = Guid.NewGuid().ToString();

        // Store invitation (you'll need to create a ParentChildInvitation entity)
        // For now, we'll send the email directly

        // Send invitation email
        await _emailService.SendParentLinkInvitationAsync(
            child.Email,
            child.FullName,
            parent.FullName,
            inviteToken);

        return Unit.Value;
    }
}

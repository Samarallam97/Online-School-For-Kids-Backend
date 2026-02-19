using Domain.Entities;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Application.Commands.Profile.Parents;

public class SendChildInviteCommand : IRequest<Unit>
{
    public string ParentUserId { get; set; } = string.Empty;
    public string ChildId { get; set; } = string.Empty;
}

public class SendChildInviteCommandHandler : IRequestHandler<SendChildInviteCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService; 
    private readonly IConfiguration _configuration;

    public SendChildInviteCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration=configuration;
    }

    public async Task<Unit> Handle(SendChildInviteCommand request, CancellationToken cancellationToken)
    {
        // Verify parent
        var parent = await _userRepository.GetByIdAsync(request.ParentUserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        // Verify child
        var child = await _userRepository.GetByIdAsync(request.ChildId);
        if (child == null)
            throw new KeyNotFoundException("Child not found");

        if (child.Role != UserRole.Student)
            throw new InvalidOperationException("User is not a student");

        // Check if already linked to another parent
        if (!string.IsNullOrEmpty(child.ParentId) && child.ParentId != request.ParentUserId)
            throw new InvalidOperationException("This child is already linked to another parent");

        var inviteToken = Guid.NewGuid().ToString();

        if (parent.ChildInvitaions is not null)
            parent.ChildInvitaions?.Add(inviteToken);
        else
            parent.ChildInvitaions = new() { inviteToken };


            await _userRepository.UpdateAsync(parent.Id, parent, cancellationToken);

        var verificationLink = $"{_configuration["FrontUrl"]}/student/accept-invite?token={inviteToken}";

        // Send invitation email
        await _emailService.SendParentLinkInvitationAsync(
            child.Email,
            child.FullName,
            parent.FullName,
            verificationLink);

        return Unit.Value;
    }
}

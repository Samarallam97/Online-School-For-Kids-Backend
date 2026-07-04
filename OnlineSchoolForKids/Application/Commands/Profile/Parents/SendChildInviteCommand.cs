using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;


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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SendChildInviteCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(SendChildInviteCommand request, CancellationToken cancellationToken)
    {
        // Verify parent
        var parent = await _userRepository.GetByIdAsync(request.ParentUserId);
        if (parent == null)
            throw new KeyNotFoundException(_localizer["ParentNotFound"]);

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException(_localizer["UserIsNotParent"]);

        // Verify child
        var child = await _userRepository.GetByIdAsync(request.ChildId);
        if (child == null)
            throw new KeyNotFoundException(_localizer["ChildNotFound"]);

        if (child.Role != UserRole.Student)
            throw new InvalidOperationException(_localizer["UserIsNotStudent"]);

        // Check if already linked to another parent
        if (!string.IsNullOrEmpty(child.ParentId) && child.ParentId != request.ParentUserId)
            throw new InvalidOperationException(_localizer["ChildAlreadyLinkedToAnotherParent"]);

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

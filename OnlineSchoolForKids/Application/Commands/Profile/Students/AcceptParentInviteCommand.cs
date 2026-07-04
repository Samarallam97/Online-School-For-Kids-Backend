using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Students;

public class AcceptInviteRequest
{
    public string Token { get; set; } = string.Empty;
}

public class AcceptParentInviteCommand : IRequest<AcceptParentInviteDto>
{
    public string Token { get; set; } = string.Empty;
    public string ChildUserId { get; set; } = string.Empty;
}

public class AcceptParentInviteCommandHandler : IRequestHandler<AcceptParentInviteCommand, AcceptParentInviteDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AcceptParentInviteCommandHandler(
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

    public async Task<AcceptParentInviteDto> Handle(AcceptParentInviteCommand request, CancellationToken cancellationToken)
    {
        // Find parent with this invitation token using repository method
        var parent = await _userRepository.GetByChildInviteTokenAsync(request.Token, cancellationToken);

        if (parent == null)
            throw new KeyNotFoundException(_localizer["InvalidOrExpiredInvitationToken"]);

        // Verify child
        var child = await _userRepository.GetByIdAsync(request.ChildUserId, cancellationToken);

        if (child == null)
            throw new KeyNotFoundException(_localizer["ChildNotFound"]);

        if (child.Role != UserRole.Student)
            throw new InvalidOperationException(_localizer["UserIsNotStudent"]);

        // Check if already linked to another parent
        if (!string.IsNullOrEmpty(child.ParentId) && child.ParentId != parent.Id)
            throw new InvalidOperationException(_localizer["ChildAlreadyLinkedToAnotherParent"]);

        // Link child to parent
        child.ParentId = parent.Id;
        await _userRepository.UpdateAsync(child.Id, child, cancellationToken);

        // Remove the used token
        parent.ChildInvitaions?.Remove(request.Token);

        if (parent.ChildrenIds is null)
            parent.ChildrenIds = new() { child.Id };
        else
            parent.ChildrenIds?.Add(child.Id);
        await _userRepository.UpdateAsync(parent.Id, parent, cancellationToken);

        // Send notification email to parent
        var childProgressUrl = $"{_configuration["FrontUrl"]}/parent/child-progress/{child.Id}";
        await _emailService.SendParentLinkAcceptedNotificationAsync(
            parent.Email,
            parent.FullName,
            child.FullName,
            childProgressUrl);

        return new AcceptParentInviteDto
        {
            Message = _localizer["AccountLinkedToParentSuccessfully"],
            ParentName = parent.FullName
        };
    }
}

public class AcceptParentInviteDto
{
    public string Message { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
}

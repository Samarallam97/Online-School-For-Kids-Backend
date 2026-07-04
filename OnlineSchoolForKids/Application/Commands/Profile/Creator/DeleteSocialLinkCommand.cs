using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Profile.Creator;

public class DeleteSocialLinkCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string LinkId { get; set; } = string.Empty;
}

public class DeleteSocialLinkCommandHandler : IRequestHandler<DeleteSocialLinkCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteSocialLinkCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(DeleteSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);


        if (user.SocialLinks == null || !user.SocialLinks.Any())
            throw new KeyNotFoundException(_localizer["NoSocialLinksFound"]);

        var socialLink = user.SocialLinks.FirstOrDefault(pm => pm.Id == request.LinkId);
        if (socialLink == null)
            throw new KeyNotFoundException(_localizer["PaymentMethodNotFound"]);

        user.SocialLinks.Remove(socialLink);

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);

        return Unit.Value;
    }
}

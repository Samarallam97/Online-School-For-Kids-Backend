using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;
namespace Application.Commands.Profile.Creator;

public class UpdateSocialLinkCommand : IRequest<SocialLinkDto>
{
    public string UserId { get; set; }
    public string LinkId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }


}

public class UpdateSocialLinkCommandHandler : IRequestHandler<UpdateSocialLinkCommand, SocialLinkDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateSocialLinkCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<SocialLinkDto> Handle(UpdateSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);

        if (user.SocialLinks == null || !user.SocialLinks.Any())
            throw new KeyNotFoundException(_localizer["NoSocialLinksFound"]);

        var socialLink = user.SocialLinks.FirstOrDefault(pm => pm.Id == request.LinkId);
        if (socialLink == null)
            throw new KeyNotFoundException(_localizer["NoSocialLinksFound"]);

        socialLink.Name = request.Name;
        socialLink.Value = request.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);


        return MapToDto(socialLink);
    }

    private SocialLinkDto MapToDto(SocialLink socialLink)
    {
        var dto = new SocialLinkDto
        {
            Id = socialLink.Id,
            Name = socialLink.Name,
            Value = socialLink.Value
        };

        return dto;
    }
}

public class SocialLinkDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

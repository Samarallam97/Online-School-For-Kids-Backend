using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Creator;

public class AddSocialLinkCommand : IRequest<SocialLinkDto>
{
    public string UserId { get; set; }
    public string Name { get; set; }

    public string Value { get; set; }
}

public class AddSocialLinkCommandHandler : IRequestHandler<AddSocialLinkCommand, SocialLinkDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AddSocialLinkCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<SocialLinkDto> Handle(AddSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);


        var socialLink = new SocialLink
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Value = request.Value,
            CreatedAt = DateTime.UtcNow
        };



        if (user.SocialLinks == null)
            user.SocialLinks = new List<SocialLink>();

        user.SocialLinks.Add(socialLink);
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

public class AddSocialLinkCommandValidator : AbstractValidator<AddSocialLinkCommand>
{
    public AddSocialLinkCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localizer["NameIsRequired"]);

        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage(localizer["ValueIsRequired"])
            .Must(BeAValidUrl).WithMessage(localizer["CvLinkMustBeValidUrl"]);


    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

using Application.DTOs.Profile;
using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

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

    public UpdateSocialLinkCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<SocialLinkDto> Handle(UpdateSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("user not found");

        if (user.SocialLinks == null || !user.SocialLinks.Any())
            throw new KeyNotFoundException("No social links found");

        var socialLink = user.SocialLinks.FirstOrDefault(pm => pm.Id == request.LinkId);
        if (socialLink == null)
            throw new KeyNotFoundException("social link not found");

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
            Name =socialLink.Name,
            Value = socialLink.Value
        };

        return dto;
    }
}

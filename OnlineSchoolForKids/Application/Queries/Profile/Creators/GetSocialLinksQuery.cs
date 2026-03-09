using Application.Commands.Profile.Creator;
using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Creators;

public class GetSocialLinksQuery : IRequest<List<SocialLinkDto>>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetSocialLinksCommandHandler : IRequestHandler<GetSocialLinksQuery, List<SocialLinkDto>>
{
    private readonly IUserRepository _userRepository;

    public GetSocialLinksCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<SocialLinkDto>> Handle(GetSocialLinksQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var socialLinks = user.SocialLinks?.Select(pm => MapToDto(pm)).ToList()
            ?? new List<SocialLinkDto>();

        return socialLinks;
    }

    private SocialLinkDto MapToDto(SocialLink socialLink)
    {
        var dto = new SocialLinkDto
        {
            Id = socialLink.Id,
            Name = socialLink.Name,
            Value = socialLink.Value,
        };


        return dto;
    }
}
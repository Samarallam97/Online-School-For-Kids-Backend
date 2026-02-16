using Application.DTOs.Profile;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

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

    public AddSocialLinkCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<SocialLinkDto> Handle(AddSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("user not found");


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
            Name =socialLink.Name,
            Value = socialLink.Value
        };

        return dto;
    }

   

    
    
}

public class AddSocialLinkCommandValidator : AbstractValidator<AddSocialLinkCommand>
{
    public AddSocialLinkCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nameis required");

        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value is required")
            .Must(BeAValidUrl).WithMessage("CV link must be a valid URL.");


    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

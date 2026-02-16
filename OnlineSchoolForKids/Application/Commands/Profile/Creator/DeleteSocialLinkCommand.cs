using Domain.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Creator;

public class DeleteSocialLinkCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string LinkId { get; set; } = string.Empty;
}

public class DeleteSocialLinkCommandHandler : IRequestHandler<DeleteSocialLinkCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public DeleteSocialLinkCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(DeleteSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("user not found");


        if (user.SocialLinks == null || !user.SocialLinks.Any())
            throw new KeyNotFoundException("No social links found");

        var socialLink = user.SocialLinks.FirstOrDefault(pm => pm.Id == request.LinkId);
        if (socialLink == null)
            throw new KeyNotFoundException("Payment method not found");

        user.SocialLinks.Remove(socialLink);

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);

        return Unit.Value;
    }
}

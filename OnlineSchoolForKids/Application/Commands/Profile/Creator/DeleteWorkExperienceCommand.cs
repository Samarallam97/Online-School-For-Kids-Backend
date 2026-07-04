using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Creator;

public class DeleteWorkExperienceCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string ExperienceId { get; set; } = string.Empty;
}

public class DeleteWorkExperienceCommandHandler : IRequestHandler<DeleteWorkExperienceCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteWorkExperienceCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(DeleteWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);

        if (user.WorkExperiences == null || !user.WorkExperiences.Any())
            throw new KeyNotFoundException(_localizer["WorkExperienceNotFound"]);

        var experienceToRemove = user.WorkExperiences.FirstOrDefault(e => e.Id == request.ExperienceId);
        if (experienceToRemove == null)
            throw new KeyNotFoundException(_localizer["WorkExperienceNotFound"]);

        user.WorkExperiences.Remove(experienceToRemove);
        await _userRepository.UpdateAsync(user.Id, user);

        return Unit.Value;
    }
}
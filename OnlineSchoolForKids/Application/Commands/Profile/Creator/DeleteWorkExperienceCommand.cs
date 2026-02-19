using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Creator;

public class DeleteWorkExperienceCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
    public string ExperienceId { get; set; } = string.Empty;
}

public class DeleteWorkExperienceCommandHandler : IRequestHandler<DeleteWorkExperienceCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public DeleteWorkExperienceCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(DeleteWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (user.WorkExperiences == null || !user.WorkExperiences.Any())
            throw new KeyNotFoundException("Work experience not found");

        var experienceToRemove = user.WorkExperiences.FirstOrDefault(e => e.Id == request.ExperienceId);
        if (experienceToRemove == null)
            throw new KeyNotFoundException("Work experience not found");

        user.WorkExperiences.Remove(experienceToRemove);
        await _userRepository.UpdateAsync(user.Id , user);

        return Unit.Value;
    }
}
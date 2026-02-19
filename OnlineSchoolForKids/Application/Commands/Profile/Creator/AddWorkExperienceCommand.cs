using Application.DTOs.Profile;
using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Creator;

public class AddWorkExperienceCommand : IRequest<WorkExperienceDto>
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public bool IsCurrentRole { get; set; }
}

public class AddWorkExperienceCommandHandler : IRequestHandler<AddWorkExperienceCommand, WorkExperienceDto>
{
    private readonly IUserRepository _userRepository;

    public AddWorkExperienceCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<WorkExperienceDto> Handle(AddWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var workExperience = new WorkExperience
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Place = request.Place,
            StartDate = request.StartDate,
            EndDate = request.IsCurrentRole ? null : request.EndDate,
            IsCurrentRole = request.IsCurrentRole
        };

        if (user.WorkExperiences == null)
            user.WorkExperiences = new List<WorkExperience>();

        user.WorkExperiences.Add(workExperience);
        await _userRepository.UpdateAsync(user.Id,user);

        return new WorkExperienceDto
        {
            Id = workExperience.Id,
            Title = workExperience.Title,
            Place = workExperience.Place,
            StartDate = workExperience.StartDate,
            EndDate = workExperience.EndDate,
            IsCurrentRole = workExperience.IsCurrentRole
        };
    }
}
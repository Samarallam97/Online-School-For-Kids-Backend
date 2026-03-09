using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Creator;

public class UpdateWorkExperienceCommand : IRequest<WorkExperienceDto>
{
    public string UserId { get; set; } = string.Empty;
    public string ExperienceId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Place { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public bool? IsCurrentRole { get; set; }
}

public class UpdateWorkExperienceCommandHandler : IRequestHandler<UpdateWorkExperienceCommand, WorkExperienceDto>
{
    private readonly IUserRepository _userRepository;

    public UpdateWorkExperienceCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<WorkExperienceDto> Handle(UpdateWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var experience = user.WorkExperiences?.FirstOrDefault(e => e.Id == request.ExperienceId);
        if (experience == null)
            throw new KeyNotFoundException("Work experience not found");

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Title))
            experience.Title = request.Title;

        if (!string.IsNullOrEmpty(request.Place))
            experience.Place = request.Place;

        if (!string.IsNullOrEmpty(request.StartDate))
            experience.StartDate = request.StartDate;

        if (request.IsCurrentRole.HasValue)
        {
            experience.IsCurrentRole = request.IsCurrentRole.Value;
            if (request.IsCurrentRole.Value)
            {
                experience.EndDate = null;
            }
            else if (!string.IsNullOrEmpty(request.EndDate))
            {
                experience.EndDate = request.EndDate;
            }
        }
        else if (!string.IsNullOrEmpty(request.EndDate))
        {
            experience.EndDate = request.EndDate;
        }

        await _userRepository.UpdateAsync(user.Id , user);

        return new WorkExperienceDto
        {
            Id = experience.Id,
            Title = experience.Title,
            Place = experience.Place,
            StartDate = experience.StartDate,
            EndDate = experience.EndDate,
            IsCurrentRole = experience.IsCurrentRole
        };
    }
}

public class WorkExperienceDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty; // Format: "YYYY-MM"
    public string? EndDate { get; set; } // Format: "YYYY-MM" or null if current
    public bool IsCurrentRole { get; set; }
}

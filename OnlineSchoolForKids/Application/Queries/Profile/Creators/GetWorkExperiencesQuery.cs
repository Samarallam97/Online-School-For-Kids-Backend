using Application.Commands.Profile.Creator;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Creators;

public class GetWorkExperiencesQuery : IRequest<List<WorkExperienceDto>>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetWorkExperiencesQueryHandler : IRequestHandler<GetWorkExperiencesQuery, List<WorkExperienceDto>>
{
    private readonly IUserRepository _userRepository;

    public GetWorkExperiencesQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<WorkExperienceDto>> Handle(GetWorkExperiencesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (user.WorkExperiences == null || !user.WorkExperiences.Any())
            return new List<WorkExperienceDto>();

        return user.WorkExperiences.Select(exp => new WorkExperienceDto
        {
            Id = exp.Id,
            Title = exp.Title,
            Place = exp.Place,
            StartDate = exp.StartDate,
            EndDate = exp.EndDate,
            IsCurrentRole = exp.IsCurrentRole
        }).ToList();
    }
}
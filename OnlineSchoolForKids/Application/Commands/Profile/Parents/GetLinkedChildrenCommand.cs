using Application.DTOs.Profile;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Parents;

public class GetLinkedChildrenCommand : IRequest<List<ChildDto>>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetLinkedChildrenQueryHandler : IRequestHandler<GetLinkedChildrenCommand, List<ChildDto>>
{
    private readonly IUserRepository _userRepository;

    public GetLinkedChildrenQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<ChildDto>> Handle(GetLinkedChildrenCommand request, CancellationToken cancellationToken)
    {
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        var children = new List<ChildDto>();

        if (parent.ChildrenIds != null && parent.ChildrenIds.Any())
        {
            foreach (var childId in parent.ChildrenIds)
            {
                var child = await _userRepository.GetByIdAsync(childId);
                if (child != null)
                {
                    children.Add(new ChildDto
                    {
                        Id = child.Id,
                        Name = child.FullName,
                        Age = CalculateAge(child.DateOfBirth),
                        ProfilePictureUrl = child.ProfilePictureUrl,
                        Courses = child.EnrolledCourseIds?.Count ?? 0
                    });
                }
            }
        }

        return children;
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}

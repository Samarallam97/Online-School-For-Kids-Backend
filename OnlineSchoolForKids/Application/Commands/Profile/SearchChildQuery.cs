using Application.Interfaces;
using Application.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

public class SearchChildQuery : IRequest<SearchChildDto>
{
    public string ParentUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class SearchChildQueryHandler : IRequestHandler<SearchChildQuery, SearchChildDto>
{
    private readonly IUserRepository _userRepository;

    public SearchChildQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<SearchChildDto> Handle(SearchChildQuery request, CancellationToken cancellationToken)
    {
        // Verify parent exists
        var parent = await _userRepository.GetByIdAsync(request.ParentUserId);
        if (parent == null)
            throw new KeyNotFoundException("Parent not found");

        if (parent.Role != Domain.Enums.UserRole.Parent)
            throw new UnauthorizedAccessException("User is not a parent");

        // Search for child by email
        var child = await _userRepository.GetByEmailAsync(request.Email);

        if (child == null)
        {
            return new SearchChildDto
            {
                Exists = false
            };
        }

        // Check if child is a student
        if (child.Role != Domain.Enums.UserRole.Student)
        {
            throw new InvalidOperationException("The email belongs to a non-student account");
        }

        // Calculate age
        var age = CalculateAge(child.DateOfBirth);

        return new SearchChildDto
        {
            Exists = true,
            Child = new ChildProfileDto
            {
                Id = child.Id,
                FullName = child.FullName,
                Email = child.Email,
                Age = age,
                ProfilePictureUrl = child.ProfilePictureUrl,
                IsAlreadyLinked = !string.IsNullOrEmpty(child.ParentId),
                CurrentParentId = child.ParentId
            }
        };
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}


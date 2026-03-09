using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Students;

public class GetLinkedParentQuery : IRequest<ParentInfoDto?>
{
    public string StudentUserId { get; set; } = string.Empty;
}


public class GetLinkedParentQueryHandler : IRequestHandler<GetLinkedParentQuery, ParentInfoDto?>
{
    private readonly IUserRepository _userRepository;

    public GetLinkedParentQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ParentInfoDto?> Handle(GetLinkedParentQuery request, CancellationToken cancellationToken)
    {
        // Get student
        var student = await _userRepository.GetByIdAsync(request.StudentUserId, cancellationToken);

        if (student == null)
            throw new KeyNotFoundException("Student not found");

        if (student.Role != UserRole.Student)
            throw new InvalidOperationException("User is not a student");

        // Check if student has a linked parent
        if (string.IsNullOrEmpty(student.ParentId))
            return null;

        // Get parent details
        var parent = await _userRepository.GetByIdAsync(student.ParentId, cancellationToken);

        if (parent == null)
            return null;

        return new ParentInfoDto
        {
            ParentId = parent.Id,
            ParentName = parent.FullName,
            ParentEmail = parent.Email,
            ParentProfilePictureUrl = parent.ProfilePictureUrl,
            LinkedSince = student.UpdatedAt // You might want to track this separately
        };
    }
}

public class ParentInfoDto
{
    public string ParentId { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string? ParentProfilePictureUrl { get; set; }
    public DateTime? LinkedSince { get; set; }
}
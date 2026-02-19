using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Students;

public class UnlinkParentCommand : IRequest<Unit>
{
    public string StudentUserId { get; set; } = string.Empty;
}

public class UnlinkParentCommandHandler : IRequestHandler<UnlinkParentCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public UnlinkParentCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(UnlinkParentCommand request, CancellationToken cancellationToken)
    {
        // Get student
        var student = await _userRepository.GetByIdAsync(request.StudentUserId, cancellationToken);

        if (student == null)
            throw new KeyNotFoundException("Student not found");

        if (student.Role != UserRole.Student)
            throw new InvalidOperationException("User is not a student");

        if (string.IsNullOrEmpty(student.ParentId))
            throw new InvalidOperationException("No parent is currently linked to this account");

        var parent = await _userRepository.GetByIdAsync(student.ParentId, cancellationToken);
        
        if (parent == null)
            throw new InvalidOperationException("Parent not found");

        // Unlink parent
        student.ParentId = null;
        await _userRepository.UpdateAsync(student.Id, student, cancellationToken);

        parent.ChildrenIds?.Remove(student.Id);
        await _userRepository.UpdateAsync(parent.Id, parent, cancellationToken);


        return Unit.Value;
    }
}
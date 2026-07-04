using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Profile.Students;

public class UnlinkParentCommand : IRequest<Unit>
{
    public string StudentUserId { get; set; } = string.Empty;
}

public class UnlinkParentCommandHandler : IRequestHandler<UnlinkParentCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UnlinkParentCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(UnlinkParentCommand request, CancellationToken cancellationToken)
    {
        // Get student
        var student = await _userRepository.GetByIdAsync(request.StudentUserId, cancellationToken);

        if (student == null)
            throw new KeyNotFoundException(_localizer["StudentNotFound"]);

        if (student.Role != UserRole.Student)
            throw new InvalidOperationException(_localizer["UserIsNotStudent"]);

        if (string.IsNullOrEmpty(student.ParentId))
            throw new InvalidOperationException(_localizer["NoParentLinkedToAccount"]);

        var parent = await _userRepository.GetByIdAsync(student.ParentId, cancellationToken);

        if (parent == null)
            throw new InvalidOperationException(_localizer["ParentNotFound"]);

        // Unlink parent
        student.ParentId = null;
        await _userRepository.UpdateAsync(student.Id, student, cancellationToken);

        parent.ChildrenIds?.Remove(student.Id);
        await _userRepository.UpdateAsync(parent.Id, parent, cancellationToken);


        return Unit.Value;
    }
}
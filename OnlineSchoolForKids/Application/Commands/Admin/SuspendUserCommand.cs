using Application.Queries.Admin;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Admin;

public record SuspendUserCommand(string UserId) : IRequest<AdminUserDto>;

// ── Handler ────────────────────────────────────────────────────────────────

public class SuspendUserCommandHandler : IRequestHandler<SuspendUserCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepo;

    public SuspendUserCommandHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<AdminUserDto> Handle(SuspendUserCommand request, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' not found.");

        if (user.Status == UserStatus.Suspended)
            throw new InvalidOperationException("User is already suspended.");

        user.Status = UserStatus.Suspended;

        await _userRepo.UpdateAsync(user.Id, user, ct);

        return new AdminUserDto
        {
            Id         = user.Id,
            Name       = user.FullName,
            Email      = user.Email,
            Role       = user.Role.ToString(),
            Status     = user.Status.ToString().ToLower(),
            JoinedDate = user.CreatedAt,
        };
    }
}
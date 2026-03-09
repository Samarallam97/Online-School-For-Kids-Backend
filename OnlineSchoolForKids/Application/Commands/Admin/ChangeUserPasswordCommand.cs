using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Admin;

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
public record ChangeUserPasswordCommand(string UserId, string NewPassword) : IRequest;


public class ChangeUserPasswordCommandHandler : IRequestHandler<ChangeUserPasswordCommand>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;

    public ChangeUserPasswordCommandHandler(
        IUserRepository userRepo,
        IPasswordHasher hasher)
    {
        _userRepo = userRepo;
        _hasher   = hasher;
    }

    public async Task Handle(ChangeUserPasswordCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var user = await _userRepo.GetByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' not found.");

        user.PasswordHash = _hasher.HashPassword(request.NewPassword);

        await _userRepo.UpdateAsync(user.Id, user, ct);
    }
}


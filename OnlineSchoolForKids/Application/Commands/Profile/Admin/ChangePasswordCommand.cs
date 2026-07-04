using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Admin;

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : IRequest;


public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _localizer = localizer;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException(_localizer["PasswordsMismatch"]);

        if (request.NewPassword.Length < 8)
            throw new ArgumentException(_localizer["PasswordMinLength"]);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["UserNotFound"]);

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException(_localizer["CurrentPasswordIncorrect"]);

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // Log the password change activity
        user.ActivityLog ??= [];
        user.ActivityLog.Add(new ActivityLogEntry
        {
            Action = "Password Changed",
            Details = "Admin changed their account password",
            Timestamp = DateTime.UtcNow
        });

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
    }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
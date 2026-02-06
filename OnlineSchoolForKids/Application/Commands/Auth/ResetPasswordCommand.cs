using Application.Interfaces;
using Application.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Auth;

public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : IRequest<Result<string>>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return Result<string>.Failure("Passwords do not match.");
        }

        var user = await _userRepository.GetByPasswordResetTokenAsync(request.Token, cancellationToken);

        if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return Result<string>.Failure("Invalid or expired reset token.");
        }

        // Update password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        // Revoke all existing refresh tokens for security
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, cancellationToken);

        return Result<string>.Success("Password has been reset successfully.");
    }
}


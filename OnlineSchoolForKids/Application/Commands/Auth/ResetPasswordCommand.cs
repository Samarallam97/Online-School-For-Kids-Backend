using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Auth;

public record ResetPasswordRequest(string Token, string NewPassword);

public record ResetPasswordCommand(
    string Token,
    string Password
) : IRequest<Result<string>>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _localizer = localizer;
    }

    public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(request.Token, cancellationToken);

        if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return Result<string>.Failure(_localizer["InvalidOrExpiredResetToken"]);
        }

        // Update password
        user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        // Revoke all existing refresh tokens for security
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, cancellationToken);

        return Result<string>.Success(_localizer["PasswordResetSuccessful"]);
    }
}


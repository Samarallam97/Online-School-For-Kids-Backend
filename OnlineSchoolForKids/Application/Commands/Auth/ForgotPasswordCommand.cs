using Application.DTOs;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Application.Commands.Auth;

public record ForgotPasswordRequest(string Email);

public record ForgotPasswordCommand(string Email) : IRequest<Result<string>>;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;


    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration
        )
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;

    }

    public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower(), cancellationToken);

        // For security, always return success even if user doesn't exist
        if (user == null || user.AuthProvider != AuthProvider.Local)
        {
            return Result<string>.Success("If the email exists, a reset link has been sent.");
        }

        // Generate reset token
        var resetToken = Guid.NewGuid().ToString();
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        // Send reset email (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var resetLink = $"{_configuration["FrontUrl"]}/reset-password?token={resetToken}";
                await _emailService.SendPasswordResetEmailAsync(user.Email,user.FullName,user.PasswordResetTokenExpiry.Value, resetLink, cancellationToken);
            }
            catch
            {
                // Log error but don't fail the request
            }
        }, cancellationToken);

        return Result<string>.Success("If the email exists, a reset link has been sent.");
    }
}


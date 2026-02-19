using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;

namespace Application.Commands.Auth;

public record VerifyEmailRequest(string Token);

public record VerifyEmailCommand(string Token) : IRequest<Result<string>>;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<Result<string>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailVerificationTokenAsync(request.Token, cancellationToken);

        if (user == null || user.EmailVerificationTokenExpiry == null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
        {
            return Result<string>.Failure("Invalid or expired verification token.");
        }

        if (user.EmailVerified)
        {
            return Result<string>.Success("Email is already verified.");
        }

        // Verify email
        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        // Send welcome email
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);
            }
            catch
            {
                // Log but don't fail
            }
        }, cancellationToken);

        return Result<string>.Success("Email verified successfully!");
    }
}



using Application.Interfaces;
using Application.Models;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Auth;

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

public record ResendVerificationEmailCommand(string Email) : IRequest<Result<string>>;

public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ResendVerificationEmailCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;

    }

    public async Task<Result<string>> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower(), cancellationToken);

        if (user == null)
        {
            return Result<string>.Success("If the email exists, a verification link has been sent.");
        }

        if (user.EmailVerified)
        {
            return Result<string>.Failure("Email is already verified.");
        }

        // Generate new token
        var verificationToken = Guid.NewGuid().ToString();
        user.EmailVerificationToken = verificationToken;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        // Send email
        _ = Task.Run(async () =>
        {
            try
            {
                var verificationLink = $"{_configuration["FrontUrl"]}/verify-email?token={verificationToken}";
                await _emailService.SendVerificationEmailAsync(user.Email,user.FullName,user.EmailVerificationTokenExpiry.Value, verificationLink, cancellationToken);
            }
            catch
            {
                // Log error
            }
        }, cancellationToken);

        return Result<string>.Success("Verification email sent.");
    }
}


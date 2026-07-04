using Application.DTOs;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Auth;

public record ResendVerificationRequest(string Email);

public record ResendVerificationEmailCommand(string Email) : IRequest<Result<string>>;

public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ResendVerificationEmailCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService, IConfiguration configuration
        , IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<Result<string>> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLower(), cancellationToken);

        if (user == null)
        {
            return Result<string>.Success(_localizer["VerificationLinkSentIfEmailExists"]);
        }

        if (user.EmailVerified)
        {
            return Result<string>.Failure(_localizer["EmailAlreadyVerified"]);
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
                await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, user.EmailVerificationTokenExpiry.Value, verificationLink, cancellationToken);
            }
            catch
            {
                // Log error
            }
        }, cancellationToken);

        return Result<string>.Success(_localizer["VerificationEmailSent"]);
    }
}

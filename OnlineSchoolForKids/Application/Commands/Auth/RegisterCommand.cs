using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Application.Commands.Auth;

public record RegisterRequest
{
    public string FullName { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
    public UserRole Role { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Country { get; init; }

    // Optional for Content Creators and Specialists
    public string? Expertise { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? CvLink { get; init; }
}

public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    DateTime DateOfBirth,
    string Country,
    string? Expertise,
    string? PortfolioUrl,
    string? CvLink
) : IRequest<Result<AuthResponse>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(localizer["NameIsRequired"])
            .MinimumLength(2).WithMessage(localizer["ValidationNameLength"])
            .MaximumLength(100).WithMessage(localizer["ValidationNameLength"])
            .Matches(@"^[\p{L}\s]+$").WithMessage(localizer["ValidationNameAllowedCharacters"]);

        RuleFor(x => x.Email)
           .NotEmpty().WithMessage(localizer["EmailIsRequired"])
           .EmailAddress().WithMessage(localizer["InvalidEmailFormat"])
           .MaximumLength(255).WithMessage(localizer["validationEmailMaxLength"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(localizer["PasswordIsRequired"])
            .MinimumLength(8).WithMessage(localizer["PasswordMinLength"])
            .MaximumLength(100).WithMessage(localizer["PasswordMaxLength"])
            .Matches(@"[A-Z]").WithMessage(localizer["ValidationPasswordUppercaseRequired"])
            .Matches(@"[a-z]").WithMessage(localizer["ValidationPasswordLowercaseRequired"])
            .Matches(@"\d").WithMessage(localizer["ValidationPasswordNumberRequired"])
            .Matches(@"[@$!%*?&#]").WithMessage(localizer["ValidationPasswordSpecialCharRequired"]);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage(localizer["InvalidRoleSelected"]);

        // Required for all users
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage(localizer["DOBIsRequired"])
            .Must(BeAtLeast3YearsOld).WithMessage(localizer["MinimumAgeForRegistration"]);

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage(localizer["CountryIsRequired"])
            .MinimumLength(2).WithMessage(localizer["ValidationCountryLength"])
            .MaximumLength(100).WithMessage(localizer["ValidationCountryLength"]);

        // Required fields for Content Creators and Specialists
        When(x => x.Role == UserRole.ContentCreator || x.Role == UserRole.Specialist, () =>
        {
            RuleFor(x => x.Expertise)
                .NotEmpty().WithMessage(localizer["ExpertiseRequiredForCreatorsAndSpecialists"])
                .MinimumLength(2).WithMessage(localizer["ExpertiseMinLength"])
                .MaximumLength(200).WithMessage(localizer["ExpertiseMaxLength"]);

            RuleFor(x => x.CvLink)
                .NotEmpty().WithMessage(localizer["CvLinkRequiredForCreatorsAndSpecialists"])
                .Must(BeAValidUrl).WithMessage(localizer["CvLinkMustBeValidUrl"]);

            RuleFor(x => x.PortfolioUrl)
                .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.PortfolioUrl))
                .WithMessage(localizer["PortfolioUrlMustBeValid"]);
        });
    }

    private bool BeAtLeast3YearsOld(DateTime dateOfBirth)
    {

        var age = DateTime.Today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
            age--;

        return age >= 3;
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IConfiguration configuration,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Result<AuthResponse>.Failure(_localizer["EmailAlreadyExists"]);
        }

        var verificationToken = Guid.NewGuid().ToString();

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLower(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            AuthProvider = AuthProvider.Local,
            DateOfBirth = request.DateOfBirth,
            Country = request.Country,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Role == UserRole.ContentCreator || request.Role == UserRole.Specialist)
        {
            user.Status = UserStatus.Pending;
            user.ExpertiseTags = new() { request.Expertise };
            user.PortfolioUrl = request.PortfolioUrl;
            user.CvLink = request.CvLink;
        }

        await _userRepository.CreateAsync(user, cancellationToken);

        // Send verification email (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var verificationLink = $"{_configuration["FrontUrl"]}/verify-email?token={verificationToken}";
                await _emailService.SendVerificationEmailAsync(
                    user.Email,
                    user.FullName,
                    user.EmailVerificationTokenExpiry.Value,
                    verificationLink,
                    cancellationToken);
            }
            catch
            {

            }
        }, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            User = Helper.MapToUserDto(user),
        });
    }


}





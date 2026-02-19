using Application.DTOs;
using Domain.Entities;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;

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
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one number.")
            .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character.");


        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role selected.");

        // Required for all users
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(BeAtLeast3YearsOld).WithMessage("You must be at least 3 years old to register.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MinimumLength(2).WithMessage("Country must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        // Required fields for Content Creators and Specialists
        When(x => x.Role == UserRole.ContentCreator || x.Role == UserRole.Specialist, () =>
        {
            RuleFor(x => x.Expertise)
                .NotEmpty().WithMessage("Area of expertise is required for content creators and specialists.")
                .MinimumLength(2).WithMessage("Expertise must be at least 2 characters.")
                .MaximumLength(200).WithMessage("Expertise must not exceed 200 characters.");

            RuleFor(x => x.CvLink)
                .NotEmpty().WithMessage("CV link is required for content creators and specialists.")
                .Must(BeAValidUrl).WithMessage("CV link must be a valid URL.");

            RuleFor(x => x.PortfolioUrl)
                .Must(BeAValidUrl).When(x => !string.IsNullOrEmpty(x.PortfolioUrl))
                .WithMessage("Portfolio URL must be a valid URL.");
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

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Result<AuthResponse>.Failure("Email is already registered.");
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
            user.ExpertiseTags = new ( ) { request.Expertise };
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





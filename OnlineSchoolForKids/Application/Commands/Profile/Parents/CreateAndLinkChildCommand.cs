using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Parents;

public class CreateAndLinkChildCommand : IRequest<ChildDto>
{
    public string ParentUserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Country { get; set; } = string.Empty;
}

public class CreateAndLinkChildCommandHandler : IRequestHandler<CreateAndLinkChildCommand, ChildDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateAndLinkChildCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService, IConfiguration configuration,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<ChildDto> Handle(CreateAndLinkChildCommand request, CancellationToken cancellationToken)
    {
        // Verify parent
        var parent = await _userRepository.GetByIdAsync(request.ParentUserId);
        if (parent == null)
            throw new KeyNotFoundException(_localizer["ParentNotFound"]);

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException(_localizer["UserIsNotParent"]);

        // Validate passwords match
        if (request.Password != request.ConfirmPassword)
            throw new ArgumentException(_localizer["PasswordsDoNotMatch"]);

        if (request.Password.Length < 6)
            throw new ArgumentException(_localizer["PasswordMin"]);

        // Check if email already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            throw new InvalidOperationException(_localizer["EmailAlreadyExists"]);

        // Validate age (must be under 18)
        var age = CalculateAge(request.DateOfBirth);
        if (age >= 18)
            throw new ArgumentException(_localizer["ChildAgeRange"]);

        var verificationToken = Guid.NewGuid().ToString();

        // Create child account
        var child = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = UserRole.Student,
            DateOfBirth = request.DateOfBirth,
            Country = request.Country,
            ParentId = request.ParentUserId,
            Status = UserStatus.Active,
            EmailVerified = false,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            EmailVerificationToken = verificationToken,
            IsFirstLogin = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Hash password
        child.PasswordHash = _passwordHasher.HashPassword(request.Password);

        // Save child
        await _userRepository.CreateAsync(child);

        // Update parent's children list
        if (parent.ChildrenIds == null)
            parent.ChildrenIds = new List<string>();

        if (!parent.ChildrenIds.Contains(child.Id))
        {
            parent.ChildrenIds.Add(child.Id);
            parent.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(parent.Id, parent);
        }

        // Send verification email
        // Store token (you'll need to implement token storage)
        var verificationLink = $"{_configuration["FrontUrl"]}/verify-email?token={verificationToken}";


        await _emailService.SendVerificationEmailAsync(
            child.Email,
            child.FullName,
            child.EmailVerificationTokenExpiry.Value,
            verificationLink);

        return new ChildDto
        {
            Id = child.Id,
            Name = child.FullName,
            Age = age,
            ProfilePictureUrl = child.ProfilePictureUrl,
            Courses = child.EnrolledCourseIds?.Count ?? 0
        };
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}

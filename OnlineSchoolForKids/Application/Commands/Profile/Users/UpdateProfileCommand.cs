using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using FluentValidation;
using MediatR;

namespace Application.Commands.Profile.Users;

public class UpdateProfileRequest
{
    // Common fields
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? Bio { get; set; }

    // student , parent
    public string? LearningGoals { get; set; }

    // creator , specialist
    public List<string>? ExpertiseTags { get; set; }

    // Specialist
    public string? ProfessionalTitle { get; set; }
    public int? YearsOfExperience { get; set; }
}

public class UpdateProfileCommand : IRequest<BaseProfileDto>
{
    public string UserId { get; set; } = string.Empty;

    // Common fields
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? Bio { get; set; }

    // student , parent 
    public string? LearningGoals { get; set; }


    // Content Creator , Specialist
    public List<string>? ExpertiseTags { get; set; }
    // Specialist
    public string? ProfessionalTitle { get; set; }
    public int? YearsOfExperience { get; set; }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, BaseProfileDto>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<BaseProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        UpdateCommonFields(user, request);

        UpdateRoleSpecificFields(user, request);

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id , user);

        return Helper.MapToProfileDto(user);
    }

    private void UpdateCommonFields(User user, UpdateProfileCommand request)
    {
        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName.Trim();

        if (!string.IsNullOrEmpty(request.Phone))
            user.Phone = request.Phone.Trim();

        if (!string.IsNullOrEmpty(request.Country))
            user.Country = request.Country.Trim();

        if (request.Bio != null)
            user.Bio = request.Bio.Trim();
    }

    private void UpdateRoleSpecificFields(User user, UpdateProfileCommand request)
    {
        switch (user.Role)
        {
            case UserRole.Student:
            case UserRole.Parent:
                UpdateStudentFields(user, request);
                break;

            case UserRole.ContentCreator:
                UpdateContentCreatorFields(user, request);
                break;

            case UserRole.Specialist:
                UpdateSpecialistFields(user, request);
                break;
        }
    }

    private void UpdateStudentFields(User user, UpdateProfileCommand request)
    {
        if (request.LearningGoals != null)
            user.LearningGoals = request.LearningGoals.Trim();
    }



    private void UpdateContentCreatorFields(User user, UpdateProfileCommand request)
    {
        if (request.ExpertiseTags != null)
            user.ExpertiseTags = request.ExpertiseTags;

    }

    private void UpdateSpecialistFields(User user, UpdateProfileCommand request)
    {
        if (!string.IsNullOrEmpty(request.ProfessionalTitle))
            user.ProfessionalTitle = request.ProfessionalTitle.Trim();

        if (request.ExpertiseTags != null)
            user.ExpertiseTags = request.ExpertiseTags;

        if (request.YearsOfExperience.HasValue)
            user.YearsOfExperience = request.YearsOfExperience.Value;
    }


}

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        When(x => !string.IsNullOrEmpty(x.FullName), () =>
        {
            RuleFor(x => x.FullName)
                .MinimumLength(2).WithMessage("Full name must be at least 2 characters")
                .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Phone), () =>
        {
            RuleFor(x => x.Phone)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
        });

        When(x => !string.IsNullOrEmpty(x.Country), () =>
        {
            RuleFor(x => x.Country)
                .MinimumLength(2).WithMessage("Country must be at least 2 characters")
                .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Bio), () =>
        {
            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters");
        });

        When(x => !string.IsNullOrEmpty(x.LearningGoals), () =>
        {
            RuleFor(x => x.LearningGoals)
                .MaximumLength(1000).WithMessage("Learning goals cannot exceed 1000 characters");
        });

        When(x => x.YearsOfExperience.HasValue, () =>
        {
            RuleFor(x => x.YearsOfExperience)
                .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be non-negative")
                .LessThanOrEqualTo(100).WithMessage("Years of experience seems unrealistic");
        });
    }
}

public class BaseProfileDto
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Phone { get; set; }
    public string Country { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsSuperAdmin { get; set; }
}


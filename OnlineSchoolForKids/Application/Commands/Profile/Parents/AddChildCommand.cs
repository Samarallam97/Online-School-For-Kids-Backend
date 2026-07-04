using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Parents;

public class AddChildCommand : IRequest<ChildDto>
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Email { get; set; }
}

public class AddChildCommandHandler : IRequestHandler<AddChildCommand, ChildDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AddChildCommandHandler(IUserRepository userRepository, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _localizer = localizer;
    }

    public async Task<ChildDto> Handle(AddChildCommand request, CancellationToken cancellationToken)
    {
        var parent = await _userRepository.GetByIdAsync(request.UserId);
        if (parent == null)
            throw new KeyNotFoundException(_localizer["ParentNotFound"]);

        if (parent.Role != UserRole.Parent)
            throw new UnauthorizedAccessException(_localizer["UserIsNotParent"]);

        // Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException(_localizer["ChildNameIsRequired"]);

        if (request.Age < 1 || request.Age > 18)
            throw new ArgumentException(_localizer["ChildAgeRange"]);

        // Check if child already exists by email
        User? child = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            child = await _userRepository.GetByEmailAsync(request.Email);

            if (child != null)
            {
                if (child.ParentId != null && child.ParentId != request.UserId)
                    throw new InvalidOperationException(_localizer["ChildAlreadyLinkedToAnotherParent"]);

                child.ParentId = request.UserId;
                await _userRepository.UpdateAsync(child.Id, child);
            }
        }

        // If child doesn't exist, create a new student account
        if (child == null)
        {
            var dateOfBirth = DateTime.UtcNow.AddYears(-request.Age);

            child = new User
            {
                FullName = request.Name,
                Email = request.Email ?? $"child_{Guid.NewGuid()}@temp.com",
                Role = UserRole.Student,
                DateOfBirth = dateOfBirth,
                Country = parent.Country,
                ParentId = request.UserId,
                Status = UserStatus.Active,
                EmailVerified = string.IsNullOrWhiteSpace(request.Email),
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(child);
        }

        // Add child to parent's children list
        if (parent.ChildrenIds == null)
            parent.ChildrenIds = new List<string>();

        if (!parent.ChildrenIds.Contains(child.Id))
        {
            parent.ChildrenIds.Add(child.Id);
            parent.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(parent.Id, parent);
        }

        return new ChildDto
        {
            Id = child.Id,
            Name = child.FullName,
            Age = CalculateAge(child.DateOfBirth),
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

public class AddChildCommandValidator : AbstractValidator<AddChildCommand>
{
    public AddChildCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage(localizer["UserIdIsRequired"]);

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localizer["ChildNameIsRequired"])
            .MaximumLength(100)
            .WithMessage(localizer["ChildNameMaxLength"]);

        RuleFor(x => x.Age)
            .InclusiveBetween(1, 18)
            .WithMessage(localizer["ChildAgeRange"]);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage(localizer["InvalidEmailAddress"]);
    }
}

public class ChildDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Avatar { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int Courses { get; set; }
}

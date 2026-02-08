using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile;

public class UpdateProfileCommand : IRequest<BaseProfileDto>
{
    public string UserId { get; set; } = string.Empty;

    // Common fields
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
    public string? Bio { get; set; }

    // Student-specific
    public string? LearningGoals { get; set; }

    // Parent-specific
    public bool? ParentalControlsActive { get; set; }
    public NotificationPreferences? NotificationPreferences { get; set; }

    // Content Creator-specific
    public List<string>? Expertise { get; set; }
    public SocialLinks? SocialLinks { get; set; }

    // Specialist-specific
    public string? ProfessionalTitle { get; set; }
    public List<string>? Specializations { get; set; }
    public int? YearsOfExperience { get; set; }
    public decimal? HourlyRate { get; set; }
    public SessionRates? SessionRates { get; set; }
    public List<AvailabilitySlotDto>? Availability { get; set; }
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

        // Update common fields
        UpdateCommonFields(user, request);

        // Update role-specific fields
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
                UpdateStudentFields(user, request);
                break;

            case UserRole.Parent:
                UpdateParentFields(user, request);
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

    private void UpdateParentFields(User user, UpdateProfileCommand request)
    {
        if (request.ParentalControlsActive.HasValue)
            user.ParentalControlsActive = request.ParentalControlsActive.Value;

        if (request.NotificationPreferences != null)
            user.NotificationPreferences = request.NotificationPreferences;
    }

    private void UpdateContentCreatorFields(User user, UpdateProfileCommand request)
    {
        if (request.Expertise != null)
            user.Expertise = request.Expertise;

        if (request.SocialLinks != null)
            user.SocialLinks = request.SocialLinks;
    }

    private void UpdateSpecialistFields(User user, UpdateProfileCommand request)
    {
        if (!string.IsNullOrEmpty(request.ProfessionalTitle))
            user.ProfessionalTitle = request.ProfessionalTitle.Trim();

        if (request.Specializations != null)
            user.Specializations = request.Specializations;

        if (request.YearsOfExperience.HasValue)
            user.YearsOfExperience = request.YearsOfExperience.Value;

        if (request.HourlyRate.HasValue)
            user.HourlyRate = request.HourlyRate.Value;

        if (request.SessionRates != null)
            user.SessionRates = request.SessionRates;

        if (request.Availability != null)
        {
            user.Availability = request.Availability.Select(a => new AvailabilitySlot
            {
                DayOfWeek = a.DayOfWeek,
                TimeSlots = a.TimeSlots
            }).ToList();
        }
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

        When(x => x.HourlyRate.HasValue, () =>
        {
            RuleFor(x => x.HourlyRate)
                .GreaterThanOrEqualTo(0).WithMessage("Hourly rate must be non-negative");
        });

        When(x => x.YearsOfExperience.HasValue, () =>
        {
            RuleFor(x => x.YearsOfExperience)
                .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be non-negative")
                .LessThanOrEqualTo(100).WithMessage("Years of experience seems unrealistic");
        });
    }
}
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application;

public class Helper
{
    public static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Role = user.Role.ToString(),
        ProfilePictureUrl = user.ProfilePictureUrl,
        IsFirstLogin = user.IsFirstLogin

    };

    public static BaseProfileDto MapToProfileDto(User user)
    {
        return user.Role switch
        {
            UserRole.Student => new StudentProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ProfilePictureUrl = user.ProfilePictureUrl,
                Phone = user.Phone,
                Country = user.Country,
                Bio = user.Bio,
                CreatedAt = user.CreatedAt,
                LearningGoals = user.LearningGoals,
                EnrolledCourses = user.EnrolledCourseIds?.Count ?? 0,
                Achievements = user.AchievementIds?.Count ?? 0,
                TotalHoursLearned = user.TotalHoursLearned ?? 0
            },

            UserRole.Parent => new ParentProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ProfilePictureUrl = user.ProfilePictureUrl,
                Phone = user.Phone,
                Country = user.Country,
                Bio = user.Bio,
                CreatedAt = user.CreatedAt,
                ChildrenCount = user.ChildrenIds?.Count ?? 0,
                ParentalControlsActive = user.ParentalControlsActive ?? false,
                NotificationPreferences = user.NotificationPreferences ?? new(),
                PaymentMethods = user.PaymentMethods?.Select(p => new PaymentMethodDto
                {
                    Id = p.Id,
                    Last4 = p.Last4,
                    Brand = p.Brand,
                    IsDefault = p.IsDefault
                })?.ToList() ?? new()
            },

            UserRole.ContentCreator => new ContentCreatorProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ProfilePictureUrl = user.ProfilePictureUrl,
                Phone = user.Phone,
                Country = user.Country,
                Bio = user.Bio,
                CreatedAt = user.CreatedAt,
                IsVerifiedCreator = user.IsVerifiedCreator ?? false,
                Expertise = user.Expertise ?? new(),
                TotalCourses = user.CreatedCourseIds?.Count ?? 0,
                TotalStudents = user.TotalStudents ?? 0,
                TotalRevenue = user.TotalRevenue ?? 0,
                AverageRating = user.AverageRating ?? 0,
                SocialLinks = user.SocialLinks,
                PayoutSettings = user.PayoutSettings
            },

            UserRole.Specialist => new SpecialistProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ProfilePictureUrl = user.ProfilePictureUrl,
                Phone = user.Phone,
                Country = user.Country,
                Bio = user.Bio,
                CreatedAt = user.CreatedAt,
                ProfessionalTitle = user.ProfessionalTitle,
                Specializations = user.Specializations ?? new(),
                Certifications = user.Certifications?.Select(c => new CertificationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Issuer = c.Issuer,
                    Year = c.Year,
                    DocumentUrl = c.DocumentUrl
                })?.ToList() ?? new(),
                YearsOfExperience = user.YearsOfExperience ?? 0,
                Availability = user.Availability?.Select(a => new AvailabilitySlotDto
                {
                    DayOfWeek = a.DayOfWeek,
                    TimeSlots = a.TimeSlots
                })?.ToList() ?? new(),
                HourlyRate = user.HourlyRate ?? 100,
                SessionRates = user.SessionRates,
                Rating = user.AverageRating ?? 0,
                StudentsHelped = user.TotalStudents ?? 0
            },

            _ => new BaseProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ProfilePictureUrl = user.ProfilePictureUrl,
                Phone = user.Phone,
                Country = user.Country,
                Bio = user.Bio,
                CreatedAt = user.CreatedAt
            }
        };
    }

};


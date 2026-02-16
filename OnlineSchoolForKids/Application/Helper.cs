using Application.DTOs;
using Application.DTOs.Profile;
using Domain.Entities;
using Domain.Enums;


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
                TotalHoursLearned = user.TotalHoursLearned ?? 0,
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
                LearningGoals = user.LearningGoals,
                TotalHoursLearned = user.TotalHoursLearned ?? 0,
                ChildrenCount = user.ChildrenIds?.Count ?? 0,
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
                TotalCourses = user.CreatedCourseIds?.Count ?? 0,
                TotalStudents = user.TotalStudents ?? 0,
                TotalRevenue = user.TotalRevenue ?? 0,
                AverageRating = user.AverageRating ?? 0,
                ExpertiseTags  = user.ExpertiseTags ?? new()
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
                ExpertiseTags = user.ExpertiseTags ?? new(),
                YearsOfExperience = user.YearsOfExperience ?? 0,
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


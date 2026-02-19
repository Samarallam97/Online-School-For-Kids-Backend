using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services;

/// <summary>
/// Service to help determine if a parent should receive notifications about a child
/// </summary>
public interface IParentNotificationService
{
    Task<bool> ShouldNotifyParentAboutProgress(string parentId, string childId);
    Task<bool> ShouldSendWeeklyReport(string parentId, string childId);
    Task<bool> ShouldNotifyAboutAchievement(string parentId, string childId);
    Task<bool> ShouldSendPaymentReminder(string parentId, string childId);
}

public class ParentNotificationService : IParentNotificationService
{
    private readonly IUserRepository _userRepository;

    public ParentNotificationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> ShouldNotifyParentAboutProgress(string parentId, string childId)
    {
        var preferences = await GetChildPreferences(parentId, childId);
        return preferences?.ProgressUpdates ?? true;
    }

    public async Task<bool> ShouldSendWeeklyReport(string parentId, string childId)
    {
        var preferences = await GetChildPreferences(parentId, childId);
        return preferences?.WeeklyReports ?? true;
    }

    public async Task<bool> ShouldNotifyAboutAchievement(string parentId, string childId)
    {
        var preferences = await GetChildPreferences(parentId, childId);
        return preferences?.AchievementAlerts ?? true;
    }

    public async Task<bool> ShouldSendPaymentReminder(string parentId, string childId)
    {
        var preferences = await GetChildPreferences(parentId, childId);
        return preferences?.PaymentReminders ?? true;
    }

    private async Task<NotificationPreferences?> GetChildPreferences(string parentId, string childId)
    {
        var parent = await _userRepository.GetByIdAsync(parentId);

        if (parent?.ChildNotificationPreferences != null &&
            parent.ChildNotificationPreferences.TryGetValue(childId, out var preferences))
        {
            return preferences;
        }

        // Return default preferences if none set
        return new NotificationPreferences
        {
            ProgressUpdates = true,
            WeeklyReports = true,
            AchievementAlerts = true,
            PaymentReminders = true
        };
    }
}

//// Example 1: Child completes a lesson
//public class LessonCompletedEventHandler
//{
//    private readonly IParentNotificationService _parentNotificationService;
//    private readonly IEmailService _emailService;
//    private readonly IUserRepository _userRepository;

//    public async Task Handle(string childId, string lessonName)
//    {
//        var child = await _userRepository.GetByIdAsync(childId);

//        // Check if child has a parent linked
//        if (!string.IsNullOrEmpty(child.ParentId))
//        {
//            // Check parent's notification preferences for this child
//            if (await _parentNotificationService.ShouldNotifyParentAboutProgress(child.ParentId, childId))
//            {
//                var parent = await _userRepository.GetByIdAsync(child.ParentId);
//                await _emailService.SendProgressUpdateEmail(
//                    parent.Email,
//                    parent.FullName,
//                    child.FullName,
//                    lessonName
//                );
//            }
//        }
//    }
//}

//// Example 2: Child earns an achievement
//public class AchievementEarnedEventHandler
//{
//    private readonly IParentNotificationService _parentNotificationService;
//    private readonly IEmailService _emailService;
//    private readonly IUserRepository _userRepository;

//    public async Task Handle(string childId, string achievementName)
//    {
//        var child = await _userRepository.GetByIdAsync(childId);

//        if (!string.IsNullOrEmpty(child.ParentId))
//        {
//            // Check if parent wants achievement notifications
//            if (await _parentNotificationService.ShouldNotifyAboutAchievement(child.ParentId, childId))
//            {
//                var parent = await _userRepository.GetByIdAsync(child.ParentId);
//                await _emailService.SendAchievementEmail(
//                    parent.Email,
//                    parent.FullName,
//                    child.FullName,
//                    achievementName
//                );
//            }
//        }
//    }
//}

//// Example 3: Weekly report generation
//public class WeeklyReportService
//{
//    private readonly IParentNotificationService _parentNotificationService;
//    private readonly IEmailService _emailService;
//    private readonly IUserRepository _userRepository;

//    public async Task SendWeeklyReports()
//    {
//        // Get all parents
//        var parents = await _userRepository.GetAllAsync(
//            user => user.Role == Domain.Enums.UserRole.Parent
//        );

//        foreach (var parent in parents)
//        {
//            // For each linked child
//            var children = await _userRepository.GetAllAsync(
//                user => user.ParentId == parent.Id
//            );

//            foreach (var child in children)
//            {
//                // Check if parent wants weekly reports for this child
//                if (await _parentNotificationService.ShouldSendWeeklyReport(parent.Id, child.Id))
//                {
//                    var reportData = await GenerateWeeklyReport(child.Id);
//                    await _emailService.SendWeeklyReportEmail(
//                        parent.Email,
//                        parent.FullName,
//                        child.FullName,
//                        reportData
//                    );
//                }
//            }
//        }
//    }

//    private async Task<object> GenerateWeeklyReport(string childId)
//    {
//        // Generate report logic
//        return new { };
//    }
//}

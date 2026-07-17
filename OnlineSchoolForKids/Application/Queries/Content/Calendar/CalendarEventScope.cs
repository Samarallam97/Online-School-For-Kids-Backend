using Domain.Entities.Content.Calendar;

namespace Application.Queries.Content.Calendar;

/// <summary>
/// Determines whether a calendar Event is relevant to a given user — i.e. whether
/// it should show up on *their* calendar. Shared by every calendar query so "my
/// calendar" means the same thing everywhere (hosting it, having joined it, or
/// being enrolled in the course it belongs to).
/// </summary>
public static class CalendarEventScope
{
    public static bool IsRelevantToUser(Event e, string userId, ICollection<string> enrolledCourseIds)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        if (e.InstructorId == userId) return true;
        if (e.Attendees.Any(a => a.UserId == userId)) return true;
        if (!string.IsNullOrEmpty(e.CourseId) && enrolledCourseIds.Contains(e.CourseId)) return true;

        return false;
    }
}
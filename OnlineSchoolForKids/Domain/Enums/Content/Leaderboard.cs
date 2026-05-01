namespace Domain.Enums.Content
{
    public enum BadgeCategory
    {
        Completion = 1,    // Course completion badges
        Streak = 2,        // Streak-based badges
        Points = 3,        // Points milestones
        Social = 4,        // Social interaction badges
        Special = 5        // Special achievements
    }

    public enum BadgeRequirementType
    {
        CoursesCompleted = 1,
        StreakDays = 2,
        TotalPoints = 3,
        QuizzesPassed = 4,
        LessonsCompleted = 5
    }
    public enum PointReason
    {
        CourseCompleted = 1,
        LessonCompleted = 2,
        QuizPassed = 3,
        StreakMaintained = 4,
        BadgeEarned = 5,
        DailyLogin = 6,
        AssignmentSubmitted = 7
    }
}

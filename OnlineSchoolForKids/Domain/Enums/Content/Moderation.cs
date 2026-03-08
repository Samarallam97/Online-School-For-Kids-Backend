namespace Domain.Enums.Content
{
    public enum ModerationStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
    public enum ContentType
    {
        Course = 1,
        Comment = 2,
        Review = 3,
        Message = 4
    }

    public enum ReportReason
    {
        Spam = 1,
        Harassment = 2,
        InappropriateContent = 3,
        Copyright = 4,
        Misinformation = 5,
        Other = 6
    }
    public enum ReportStatus
    {
        Pending = 1,
        UnderReview = 2,
        Resolved = 3,
        Dismissed = 4
    }

    public enum ModerationAction
    {
        Dismissed = 1,
        Warned = 2,
        ContentRemoved = 3,
        UserBanned = 4
    }
}

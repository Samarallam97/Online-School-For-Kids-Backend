namespace Domain.Entities.Chatbot
{
    public class PendingQuestion : BaseEntity
    {
        /// <summary>The user who asked the question. Null if asked anonymously.</summary>
        public string? UserId { get; set; }

        public string Question { get; set; } = string.Empty;

        /// <summary>"ar" or "en" — detected by the chatbot.</summary>
        public string Language { get; set; } = "en";

        /// <summary>Similarity score returned by the chatbot (below threshold).</summary>
        public double Similarity { get; set; }

        public PendingQuestionStatus Status { get; set; } = PendingQuestionStatus.Pending;

        /// <summary>The admin's answer. Null until the admin responds.</summary>
        public string? Answer { get; set; }

        /// <summary>The admin who answered. Null until answered.</summary>
        public string? AnsweredByAdminId { get; set; }

        public DateTime? AnsweredAt { get; set; }

        /// <summary>
        /// True once the Q&A has been successfully pushed to the chatbot's
        /// knowledge base via POST /admin/faq.
        /// </summary>
        public bool PushedToChatbot { get; set; } = false;
    }

    public enum PendingQuestionStatus
    {
        Pending,
        Answered
    }

}

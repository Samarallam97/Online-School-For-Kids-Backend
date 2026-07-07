namespace Domain.Interfaces.Services.Shared
{
    public interface IChatbotService
    {
        /// <summary>Sends a question to the AI chatbot and returns the answer.</summary>
        Task<ChatbotResponse> AskAsync(
            string query, string? lang = null, CancellationToken ct = default);

        /// <summary>
        /// Pushes a new Q&A pair to the chatbot's knowledge base via POST /admin/faq.
        /// Called after an admin answers a pending question so the chatbot can answer
        /// the same question automatically next time.
        /// Returns true if the push succeeded.
        /// </summary>
        Task<bool> AddToKnowledgeBaseAsync(
            string questionAr, string answerAr,
            string questionEn, string answerEn,
            string category,
            CancellationToken ct = default);
    }

    public record ChatbotResponse(
        bool Status,
        string Answer,
        double Similarity,
        string Language,
        double ResponseTime);

}

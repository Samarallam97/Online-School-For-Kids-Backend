namespace Domain.Entities.Content.Quiz
{
    public class QuizAnswer
    {
        public string QuestionId { get; set; } = string.Empty;
        public string SelectedOptionId { get; set; } = string.Empty;
        public bool? IsCorrect { get; set; }
        public int? PointsEarned { get; set; }
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}

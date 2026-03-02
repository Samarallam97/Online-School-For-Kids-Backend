namespace Domain.Entities.Content.Quiz
{
    public class QuizOption
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
        public int Order { get; set; }
    }
}

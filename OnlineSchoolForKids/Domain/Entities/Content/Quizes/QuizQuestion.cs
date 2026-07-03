namespace Domain.Entities.Content.Quizes
{
    public class QuizQuestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public List<QuizOption> Options { get; set; } = new();
        public string? Explanation { get; set; }
        public int CorrectAnswer { get; set; }
    }
}

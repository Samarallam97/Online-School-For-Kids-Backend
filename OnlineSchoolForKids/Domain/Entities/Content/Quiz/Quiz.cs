namespace Domain.Entities.Content.Quiz
{
    public class Quiz : BaseEntity
    {
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PassingScore { get; set; } = 70; // Percentage
        public int? TimeLimit { get; set; } // Minutes (null = unlimited)
        public int? MaxAttempts { get; set; } // null = unlimited
        public bool ShuffleQuestions { get; set; } = false;
        public bool ShowAnswers { get; set; } = true;
        public bool IsPublished { get; set; } = false;
        public List<QuizQuestion> Questions { get; set; } = new();
        public Course? Course { get; set; }
    }
   
   
}

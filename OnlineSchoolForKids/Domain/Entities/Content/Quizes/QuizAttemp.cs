using Domain.Entities.Users;
using Domain.Enums.Content;
namespace Domain.Entities.Content.Quizes;

/// <summary>
/// Records one student attempt at a lesson quiz (one difficulty level).
/// Students may retake any quiz — each attempt creates a new record.
/// </summary>
public class QuizAttempt : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;

    /// <summary>"easy" | "medium" | "hard"</summary>
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>0–100 percentage score</summary>
    public decimal? Score { get; set; }

    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }

    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Per-question breakdown stored for the review screen.</summary>
    public List<QuizAttemptAnswer> Answers { get; set; } = new();
}

public class QuizAttemptAnswer
{
    public int QuestionIndex { get; set; }
    public int SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
}

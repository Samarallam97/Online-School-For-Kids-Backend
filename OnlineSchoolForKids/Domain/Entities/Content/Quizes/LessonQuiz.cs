using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Content.Quizes;


/// <summary>
/// A full quiz set for one difficulty level, embedded inside a Lesson.
/// Students choose which difficulty they want before starting.
/// </summary>
public class LessonQuiz
{
    public string Id { get; set; } = string.Empty;

    /// <summary>"easy" | "medium" | "hard"</summary>
    public string Difficulty { get; set; } = string.Empty;

    public List<QuizQuestion> Questions { get; set; } = new();
}


using Domain.Entities.Content.Progress;
using Domain.Entities.Content.Quizes;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Course;


public class SaveLessonWithQuizCommand : IRequest<SaveLessonWithQuizResponse>
{
    public string InstructorId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;

    // Finalized lesson fields
    public string Title { get; set; } = string.Empty;
    /// <summary>Clean transcript (no timestamps) for student display.</summary>
    public string Transcript { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public int Duration { get; set; }  // seconds
    public int Order { get; set; }
    public bool IsFree { get; set; }

    // Quizzes for all three difficulty levels
    public List<DifficultyQuizDto> Quizzes { get; set; } = new();
}

public class DifficultyQuizDto
{
    /// <summary>"easy" | "medium" | "hard"</summary>
    public string Difficulty { get; set; } = string.Empty;
    public List<QuizQuestionSaveDto> Questions { get; set; } = new();
}

public class QuizQuestionSaveDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswer { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class SaveLessonWithQuizResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LessonId { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class SaveLessonWithQuizHandler
    : IRequestHandler<SaveLessonWithQuizCommand, SaveLessonWithQuizResponse>
{
    private readonly ICourseRepository _courseRepo;
    private readonly ILogger<SaveLessonWithQuizHandler> _logger;

    public SaveLessonWithQuizHandler(
        ICourseRepository courseRepo,
        ILogger<SaveLessonWithQuizHandler> logger)
    {
        _courseRepo = courseRepo;
        _logger     = logger;
    }

    public async Task<SaveLessonWithQuizResponse> Handle(
        SaveLessonWithQuizCommand request, CancellationToken ct)
    {
        try
        {
            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null || course.InstructorId != request.InstructorId)
                return Fail("Course not found");

            var section = course.Sections?.FirstOrDefault(s => s.Id == request.SectionId);
            if (section == null)
                return Fail("Section not found");

            section.Lessons ??= new List<Lesson>();

            // Find existing lesson or create new shell
            var lesson = section.Lessons.FirstOrDefault(l => l.Id == request.LessonId);
            bool isNew = lesson == null;

            if (isNew)
            {
                lesson = new Lesson { Id = ObjectId.GenerateNewId().ToString() };
                section.Lessons.Add(lesson);
            }

            // Update all lesson fields
            lesson!.CourseId    = request.CourseId;
            lesson.SectionId    = request.SectionId;
            lesson.Title        = request.Title;
            lesson.Description  = request.Transcript;   // transcript stored as description
            lesson.VideoUrl     = request.VideoUrl;
            lesson.Duration     = request.Duration;
            lesson.Order        = request.Order;
            lesson.IsFree       = request.IsFree;
            lesson.IsPublished  = true;

            // Store quizzes as embedded documents on the lesson
            // Each difficulty is a separate quiz set so the student can choose
            lesson.Quizzes = request.Quizzes.Select(q => new LessonQuiz
            {
                Id         = ObjectId.GenerateNewId().ToString(),
                Difficulty = q.Difficulty,
                Questions  = q.Questions.Select(qq => new QuizQuestion
                {
                    Id            = ObjectId.GenerateNewId().ToString(),
                    Text      = qq.Question,
                    Options       =  qq.Options.Select((o, index) => new QuizOption
                    {
                        Id        = ObjectId.GenerateNewId().ToString(),
                        Text      = o,
                        IsCorrect = index == qq.CorrectAnswer,
                        Order     = index
                    }).ToList(),
                    CorrectAnswer = qq.CorrectAnswer,
                    Explanation   = qq.Explanation
                }).ToList()
            }).ToList();

            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepo.UpdateAsync(course.Id, course, ct);

            _logger.LogInformation(
                "Lesson {LessonId} saved with {Count} quiz sets for course {CourseId}",
                lesson.Id, lesson.Quizzes.Count, request.CourseId);

            return new SaveLessonWithQuizResponse
            {
                Success  = true,
                Message  = isNew ? "Lesson created" : "Lesson updated",
                LessonId = lesson.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving lesson with quiz");
            return Fail("An error occurred");
        }
    }

    private static SaveLessonWithQuizResponse Fail(string msg) =>
        new() { Success = false, Message = msg };
}
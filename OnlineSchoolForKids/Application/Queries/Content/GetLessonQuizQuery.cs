using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Queries.Content
{
    /// <summary>
    /// Returns full quiz content (options, correct answers, explanations) for
    /// one lesson, for an enrolled student taking the quiz. Distinct from
    /// GetCourseCurriculumQuery, which deliberately only returns a per-difficulty
    /// question COUNT so answers aren't shipped to the player on initial load.
    /// </summary>
    public class GetLessonQuizQuery : IRequest<List<LessonQuizFullDto>?>
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
    }

    public class LessonQuizFullDto
    {
        public string Difficulty { get; set; } = string.Empty;
        public List<LessonQuizQuestionDto> Questions { get; set; } = new();
    }

    public class LessonQuizQuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public List<LessonQuizOptionDto> Options { get; set; } = new();
        public int CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
    }

    public class LessonQuizOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class GetLessonQuizQueryHandler
        : IRequestHandler<GetLessonQuizQuery, List<LessonQuizFullDto>?>
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;

        public GetLessonQuizQueryHandler(
            ICourseRepository courseRepo,
            IEnrollmentRepository enrollmentRepo)
        {
            _courseRepo = courseRepo;
            _enrollmentRepo = enrollmentRepo;
        }

        public async Task<List<LessonQuizFullDto>?> Handle(
            GetLessonQuizQuery request, CancellationToken ct)
        {
            // Gate on enrollment — same rule GetContinueLearningQuery uses —
            // so quiz answers can't be pulled by someone who isn't enrolled.
            var enrollment = await _enrollmentRepo.GetOneAsync(
                e => e.UserId == request.UserId && e.CourseId == request.CourseId, ct);
            if (enrollment == null) return null;

            var course = await _courseRepo.GetByIdAsync(request.CourseId, ct);
            if (course == null) return null;

            var lesson = course.Sections?
                .SelectMany(s => s.Lessons )?
                .FirstOrDefault(l => l.Id == request.LessonId);
            if (lesson == null) return null;

            return lesson.Quizzes.Select(q => new LessonQuizFullDto
            {
                Difficulty = q.Difficulty,
                Questions = q.Questions.Select(qq => new LessonQuizQuestionDto
                {
                    Text = qq.Text,
                    CorrectAnswer = qq.CorrectAnswer,
                    Explanation = qq.Explanation,
                    Options = qq.Options
                        .OrderBy(o => o.Order)
                        .Select(o => new LessonQuizOptionDto
                        {
                            Id = o.Id,
                            Text = o.Text,
                            Order = o.Order
                        }).ToList()
                }).ToList()
            }).ToList();
        }
    }
}
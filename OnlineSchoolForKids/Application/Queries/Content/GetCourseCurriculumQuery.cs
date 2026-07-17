using Domain.Entities.Content.Progress;
using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Queries.Content
{
    public class GetCourseCurriculumQuery : IRequest<CourseCurriculumDto?>
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    // NOTE: property names here must match the frontend's CourseCurriculum /
    // CurriculumSection / CurriculumLesson types in studentService.ts exactly
    // (camelCase on the wire via the default JSON serializer). The previous
    // version used SectionId/LessonId, which serialized to sectionId/lessonId —
    // the frontend reads section.id / lesson.id, which were always undefined.
    // That's why lesson selection in CoursePlayerPage always fell back to the
    // first lesson regardless of which one was clicked.

    public class CourseCurriculumDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public double ProgressPercent { get; set; }
        public List<CurriculumSectionDto> Sections { get; set; } = new();
    }

    public class CurriculumSectionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<CurriculumLessonDto> Lessons { get; set; } = new();
    }

    public class CurriculumLessonDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int Order { get; set; }
        public bool IsFree { get; set; }
        public bool IsCompleted { get; set; }
        public string? VideoUrl { get; set; }
        public string? Transcript { get; set; }
        /// <summary>Clip start offset (seconds) within VideoUrl. 0 when the lesson owns its whole video.</summary>
        public int StartTime { get; set; }
        /// <summary>Clip end offset (seconds) within VideoUrl. 0 when the lesson owns its whole video (no trim).</summary>
        public int EndTime { get; set; }
        public List<CurriculumQuizSummaryDto>? Quizzes { get; set; }
        public List<CurriculumMaterialDto>? Materials { get; set; }
    }

    /// <summary>
    /// Deliberately omits question text/options/correct answers — this endpoint
    /// only needs to tell the player which difficulty levels exist and how many
    /// questions each has, so the Quiz tab and per-lesson quiz buttons can
    /// render. Actual question content should be fetched from a dedicated,
    /// authenticated "start quiz" endpoint at attempt time, not leaked here.
    /// </summary>
    public class CurriculumQuizSummaryDto
    {
        public string Difficulty { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
    }

    public class CurriculumMaterialDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    public class GetCourseCurriculumQueryHandler
            : IRequestHandler<GetCourseCurriculumQuery, CourseCurriculumDto?>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILessonProgressRepository _progressRepository;

        public GetCourseCurriculumQueryHandler(
            ICourseRepository courseRepository,
            ILessonProgressRepository progressRepository)
        {
            _courseRepository = courseRepository;
            _progressRepository = progressRepository;
        }

        public async Task<CourseCurriculumDto?> Handle(
            GetCourseCurriculumQuery request,
            CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null) return null;

            var progress = await _progressRepository
                .GetAllAsync(p => p.UserId == request.UserId && p.CourseId == request.CourseId, cancellationToken);
            var progressLookup = progress.ToDictionary(p => p.LessonId, p => p.IsCompleted);

            var sectionDtos = (course.Sections ?? new List<Section>())
                .Where(s => s.IsPublished)
                .OrderBy(s => s.Order)
                .Select(section =>
                {
                    var sectionLessons = (section.Lessons ?? new List<Lesson>())
                        .Where(l => l.IsPublished)
                        .OrderBy(l => l.Order)
                        .ToList();

                    var lessonDtos = sectionLessons.Select(lesson => new CurriculumLessonDto
                    {
                        Id = lesson.Id,
                        Title = lesson.Title,
                        Duration = lesson.Duration,
                        Order = lesson.Order,
                        // IsPreview OR IsFree both mean the lesson is freely viewable —
                        // matches the same rule GetCourseByIdQuery uses for the catalog page.
                        IsFree = lesson.IsFree || lesson.IsPreview,
                        IsCompleted = progressLookup.TryGetValue(lesson.Id, out var done) && done,
                        VideoUrl = string.IsNullOrEmpty(lesson.VideoUrl) ? null : lesson.VideoUrl,
                        // Transcript text (with [hh:mm:ss] markers) is stored in Description —
                        // there is no separate Transcript field on the Lesson entity.
                        Transcript = lesson.Description,
                        StartTime = lesson.StartTimeSeconds,
                        EndTime = lesson.EndTimeSeconds,
                        Quizzes = lesson.Quizzes.Count > 0
                            ? lesson.Quizzes.Select(q => new CurriculumQuizSummaryDto
                            {
                                Difficulty = q.Difficulty,
                                QuestionCount = q.Questions?.Count ?? 0
                            }).ToList()
                            : null,
                        Materials = lesson.Materials.Count > 0
                            ? lesson.Materials.Select(m => new CurriculumMaterialDto
                            {
                                Id = m.Id,
                                Title = m.Title,
                                Url = m.Url,
                                Type = m.Type,
                                FileSize = m.FileSize
                            }).ToList()
                            : null,
                    }).ToList();

                    return new CurriculumSectionDto
                    {
                        Id = section.Id,
                        Title = section.Title,
                        Order = section.Order,
                        Lessons = lessonDtos
                    };
                })
                .ToList();

            var totalLessons = sectionDtos.Sum(s => s.Lessons.Count);
            var completedLessons = sectionDtos.Sum(s => s.Lessons.Count(l => l.IsCompleted));

            return new CourseCurriculumDto
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                ProgressPercent = totalLessons > 0
                    ? Math.Round((double)completedLessons / totalLessons * 100, 1)
                    : 0,
                Sections = sectionDtos
            };
        }
    }
}
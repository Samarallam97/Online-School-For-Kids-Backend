using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Queries.Content
{
    public class GetCourseCurriculumQuery : IRequest<CourseCurriculumDto>
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
    }
        public class CourseCurriculumDto
        {
            public string CourseId { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public List<SectionDto> Sections { get; set; } = new();
    }
        
public class GetCourseCurriculumQueryHandler
    : IRequestHandler<GetCourseCurriculumQuery, CourseCurriculumDto?>
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ISectionRepository _sectionRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly ILessonProgressRepository _progressRepository;

        public GetCourseCurriculumQueryHandler(
            ICourseRepository courseRepository,
            ISectionRepository sectionRepository,
            ILessonRepository lessonRepository,
            ILessonProgressRepository progressRepository)
        {
            _courseRepository = courseRepository;
            _sectionRepository = sectionRepository;
            _lessonRepository = lessonRepository;
            _progressRepository = progressRepository;
        }

        public async Task<CourseCurriculumDto?> Handle(
            GetCourseCurriculumQuery request,
            CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null) return null;

            var sections = await _sectionRepository
                .GetAllAsync(s => s.CourseId == request.CourseId, cancellationToken);

            var lessons = await _lessonRepository
                .GetAllAsync(l => l.CourseId == request.CourseId, cancellationToken);

            var progress = await _progressRepository
                .GetAllAsync(p => p.UserId == request.UserId && p.CourseId == request.CourseId, cancellationToken);

            var progressLookup = progress.ToDictionary(p => p.LessonId, p => p.IsCompleted);

            var result = new CourseCurriculumDto
            {
                CourseId = course.Id,
                Title = course.Title,
                Sections = sections
                    .OrderBy(s => s.Order)
                    .Select(section =>
                    {
                        var sectionLessons = lessons
                            .Where(l => l.SectionId == section.Id)
                            .OrderBy(l => l.Order)
                            .ToList();

                        var lessonDtos = sectionLessons.Select(lesson =>
                            new LessonDto
                            {
                                LessonId = lesson.Id,
                                Title = lesson.Title,
                                Duration = lesson.Duration,
                                IsCompleted = progressLookup.ContainsKey(lesson.Id)
                                              && progressLookup[lesson.Id]
                            }).ToList();

                        return new SectionDto
                        {
                            SectionId = section.Id,
                            Title = section.Title,
                            TotalLessons = sectionLessons.Count,
                            CompletedLessons = lessonDtos.Count(l => l.IsCompleted),
                            Lessons = lessonDtos
                        };
                    })
                    .ToList()
            };

            return result;
        }
    }
    public class SectionDto
        {
            public string SectionId { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;

            public int TotalLessons { get; set; }
            public int CompletedLessons { get; set; }

            public List<LessonDto> Lessons { get; set; } = new();
        }

        public class LessonDto
        {
            public string LessonId { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public int Duration { get; set; }

            public bool IsCompleted { get; set; }
        }
    }


using Domain.Entities.Content.Quiz;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content
{
    public class GetCourseQuizzesQuery : IRequest<IEnumerable<QuizDto>>
    {
        public string CourseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class GetCourseQuizzesHandler : IRequestHandler<GetCourseQuizzesQuery, IEnumerable<QuizDto>>
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IGenericRepository<QuizAttempt> _attemptRepository;
        private readonly ILogger<GetCourseQuizzesHandler> _logger;

        public GetCourseQuizzesHandler(
            IQuizRepository quizRepository,
            IGenericRepository<QuizAttempt> attemptRepository,
            ILogger<GetCourseQuizzesHandler> logger)
        {
            _quizRepository = quizRepository;
            _attemptRepository = attemptRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<QuizDto>> Handle(
            GetCourseQuizzesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var quizzes = await _quizRepository.GetAllAsync(
                    q => q.CourseId == request.CourseId && q.IsPublished,
                    cancellationToken);

                var attempts = await _attemptRepository.GetAllAsync(
                    a => a.CourseId == request.CourseId && a.UserId == request.UserId,
                    cancellationToken);

                return quizzes.Select(q => new QuizDto
                {
                    Id = q.Id,
                    CourseId = q.CourseId,
                    Title = q.Title,
                    Description = q.Description,
                    PassingScore = q.PassingScore,
                    TimeLimit = q.TimeLimit,
                    MaxAttempts = q.MaxAttempts,
                    TotalQuestions = q.Questions.Count,
                    TotalPoints = q.Questions.Sum(x => x.Points),
                    UserAttempts = attempts.Count(a => a.QuizId == q.Id),
                    BestScore = attempts
                        .Where(a => a.QuizId == q.Id && a.Score.HasValue)
                        .Max(a => (decimal?)a.Score)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quizzes for course {CourseId}", request.CourseId);
                return Enumerable.Empty<QuizDto>();
            }
        }
    }
    public class QuizDto
    {
        public string Id { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PassingScore { get; set; }
        public int? TimeLimit { get; set; }
        public int? MaxAttempts { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalPoints { get; set; }
        public int? UserAttempts { get; set; } // How many times user attempted
        public decimal? BestScore { get; set; } // User's best score
    }
}



using Domain.Interfaces.Repositories.Content;
using MediatR;

namespace Application.Queries.Content
{
        public class GetUserQuizResultsQuery : IRequest<List<UserQuizResultDto>>
        {
            public string UserId { get; set; } = string.Empty;
            public string CourseId { get; set; } = string.Empty;
    }

public class GetUserQuizResultsQueryHandler
    : IRequestHandler<GetUserQuizResultsQuery, List<UserQuizResultDto>>
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IAttemptRepository _quizAttemptRepository;

        public GetUserQuizResultsQueryHandler(
            IQuizRepository quizRepository,
            IAttemptRepository quizAttemptRepository)
        {
            _quizRepository = quizRepository;
            _quizAttemptRepository = quizAttemptRepository;
        }

        public async Task<List<UserQuizResultDto>> Handle(
            GetUserQuizResultsQuery request,
            CancellationToken cancellationToken)
        {
            // All quizzes in this course
            var quizzes = await _quizRepository
                .GetAllAsync(q => q.CourseId == request.CourseId, cancellationToken);

            // All attempts by this user in this course
            var attempts = await _quizAttemptRepository
                .GetAllAsync(a => a.UserId == request.UserId && a.CourseId == request.CourseId, cancellationToken);

            var result = new List<UserQuizResultDto>();

            foreach (var quiz in quizzes)
            {
                var quizAttempts = attempts
                    .Where(a => a.QuizId == quiz.Id)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToList();

                if (!quizAttempts.Any())
                    continue;

                var bestAttempt = quizAttempts.OrderByDescending(a => a.Score).FirstOrDefault();

                var totalMarks = quiz.Questions?.Sum(q => q.Points) ?? 0;

                var score = bestAttempt?.Score ?? 0;
                var percentage = totalMarks == 0 ? 0 : (decimal)score / totalMarks * 100;

                result.Add(new UserQuizResultDto
                {
                    QuizId = quiz.Id,
                    QuizTitle = quiz.Title,
                    Score = bestAttempt.Score,
                    TotalMarks = totalMarks,
                    Percentage = Math.Round(percentage, 2),
                    IsPassed = percentage >= quiz.PassingScore,
                    AttemptsCount = quizAttempts.Count,
                    LastAttemptDate = quizAttempts.First().CreatedAt
                });
            }

            return result;
        }
    }
    public class UserQuizResultDto
        {
            public string QuizId { get; set; } = string.Empty;
            public string QuizTitle { get; set; } = string.Empty;

            public decimal? Score { get; set; }
            public int TotalMarks { get; set; }

            public decimal Percentage { get; set; }
            public bool IsPassed { get; set; }

            public int AttemptsCount { get; set; }

            public DateTime? LastAttemptDate { get; set; }
        }
    }


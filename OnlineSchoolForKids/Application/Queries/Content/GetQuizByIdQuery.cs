using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Content
{
    public class GetQuizByIdQuery : IRequest<QuizDetailDto?>
    {
        public string QuizId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class GetQuizByIdHandler : IRequestHandler<GetQuizByIdQuery, QuizDetailDto?>
    {
        private readonly IQuizRepository _quizRepository;
        private readonly ILogger<GetQuizByIdHandler> _logger;

        public GetQuizByIdHandler(
            IQuizRepository quizRepository,
            ILogger<GetQuizByIdHandler> logger)
        {
            _quizRepository = quizRepository;
            _logger = logger;
        }

        public async Task<QuizDetailDto?> Handle(GetQuizByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var quiz = await _quizRepository.GetByIdAsync(request.QuizId, cancellationToken);
                if (quiz == null || !quiz.IsPublished) return null;

                return new QuizDetailDto
                {
                    Id = quiz.Id,
                    CourseId = quiz.CourseId,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    PassingScore = quiz.PassingScore,
                    TimeLimit = quiz.TimeLimit,
                    Questions = quiz.Questions.Select(q => new QuizQuestionDto
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Order = q.Order,
                        Points = q.Points,
                        Options = q.Options.Select(o => new QuizOptionDto
                        {
                            Id = o.Id,
                            Text = o.Text,
                            Order = o.Order
                            // isCorrect NOT included
                        }).OrderBy(o => o.Order).ToList()
                    }).OrderBy(q => q.Order).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz {QuizId}", request.QuizId);
                return null;
            }
        }
    }
    public class QuizDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PassingScore { get; set; }
        public int? TimeLimit { get; set; }
        public List<QuizQuestionDto> Questions { get; set; } = new();
    }

    public class QuizQuestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public int Points { get; set; }
        public List<QuizOptionDto> Options { get; set; } = new();
    }
    public class QuizOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        // isCorrect NOT included - hidden from student
    }
}
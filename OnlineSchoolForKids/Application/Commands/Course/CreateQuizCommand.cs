using Domain.Entities.Content.Quiz;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Course
{
    public class CreateQuizCommand : IRequest<CreateQuizResponse>
    {
        public CreateQuizDto Dto { get; set; } = new();
    }

    public class CreateQuizResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? QuizId { get; set; }
        public string? QuizTitle { get; set; }
        public int TotalQuestions { get; set; }
    }
    public class CreateQuizFromAiHandler : IRequestHandler<CreateQuizCommand, CreateQuizResponse>
    {
        private readonly IQuizRepository _quizRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly ILogger<CreateQuizFromAiHandler> _logger;

        public CreateQuizFromAiHandler(
            IQuizRepository quizRepository,
            ICourseRepository courseRepository,
            ILogger<CreateQuizFromAiHandler> logger)
        {
            _quizRepository = quizRepository;
            _courseRepository = courseRepository;
            _logger = logger;
        }

        public async Task<CreateQuizResponse> Handle(
            CreateQuizCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var dto = request.Dto;

                // STEP 1: Validate course exists
                var course = await _courseRepository.GetByIdAsync(dto.CourseId, cancellationToken);
                if (course == null)
                {
                    _logger.LogWarning("Course {CourseId} not found", dto.CourseId);
                    return new CreateQuizResponse
                    {
                        Success = false,
                        Message = "Course not found"
                    };
                }

                // STEP 2: Validate questions
                if (!dto.Questions.Any())
                {
                    return new CreateQuizResponse
                    {
                        Success = false,
                        Message = "Quiz must have at least one question"
                    };
                }

                // Validate each question has options and at least one correct answer
                foreach (var question in dto.Questions)
                {
                    if (!question.Options.Any())
                    {
                        return new CreateQuizResponse
                        {
                            Success = false,
                            Message = $"Question '{question.Text}' must have options"
                        };
                    }

                    if (!question.Options.Any(o => o.IsCorrect))
                    {
                        return new CreateQuizResponse
                        {
                            Success = false,
                            Message = $"Question '{question.Text}' must have at least one correct answer"
                        };
                    }
                }

                // STEP 3: Map DTO to Quiz Entity
                var quiz = new Quiz
                {
                    CourseId = dto.CourseId,
                    Title = dto.Title,
                    Description = dto.Description,
                    PassingScore = dto.PassingScore,
                    TimeLimit = dto.TimeLimit,
                    MaxAttempts = dto.MaxAttempts,
                    ShuffleQuestions = dto.ShuffleQuestions,
                    ShowAnswers = dto.ShowAnswers,
                    IsPublished = false, // Admin can publish later
                    Questions = dto.Questions.Select(q => new QuizQuestion
                    {
                        Id = Guid.NewGuid().ToString(),
                        Text = q.Text,
                        Order = q.Order,
                        Points = q.Points,
                        Explanation = q.Explanation,
                        Options = q.Options.Select(o => new QuizOption
                        {
                            Id = Guid.NewGuid().ToString(),
                            Text = o.Text,
                            IsCorrect = o.IsCorrect,
                            Order = o.Order
                        }).ToList()
                    }).ToList()
                };

                // STEP 4: Save to MongoDB
                await _quizRepository.CreateAsync(quiz, cancellationToken);

                _logger.LogInformation(
                    "Quiz created successfully: {QuizId} - {QuizTitle} with {QuestionCount} questions",
                    quiz.Id, quiz.Title, quiz.Questions.Count);

                return new CreateQuizResponse
                {
                    Success = true,
                    Message = "Quiz created successfully",
                    QuizId = quiz.Id,
                    QuizTitle = quiz.Title,
                    TotalQuestions = quiz.Questions.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quiz");
                return new CreateQuizResponse
                {
                    Success = false,
                    Message = "An error occurred while creating quiz"
                };
            }
        }
    }
}

public class CreateQuizDto
    {
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PassingScore { get; set; } = 70;
        public int? TimeLimit { get; set; } // Minutes
        public int? MaxAttempts { get; set; }
        public bool ShuffleQuestions { get; set; } = false;
        public bool ShowAnswers { get; set; } = true;
        public List<QuestionDto> Questions { get; set; } = new();
    }
    public class QuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string? Explanation { get; set; }
        public List<OptionDto> Options { get; set; } = new();
    }

    public class OptionDto
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Order { get; set; }
    }


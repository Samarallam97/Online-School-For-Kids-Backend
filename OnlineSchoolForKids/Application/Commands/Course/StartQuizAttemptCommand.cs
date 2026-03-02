using Domain.Entities.Content.Quiz;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Course
{
    public class StartQuizAttemptCommand : IRequest<StartQuizAttemptResponse>
    {
        public string QuizId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class StartQuizAttemptResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AttemptId { get; set; }
    }

    public class StartQuizAttemptHandler : IRequestHandler<StartQuizAttemptCommand, StartQuizAttemptResponse>
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IAttemptRepository _attemptRepository;
        private readonly ILogger<StartQuizAttemptHandler> _logger;

        public StartQuizAttemptHandler(
            IQuizRepository quizRepository,
            IAttemptRepository attemptRepository,
            ILogger<StartQuizAttemptHandler> logger)
        {
            _quizRepository = quizRepository;
            _attemptRepository = attemptRepository;
            _logger = logger;
        }

        public async Task<StartQuizAttemptResponse> Handle(
            StartQuizAttemptCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get quiz
                var quiz = await _quizRepository.GetByIdAsync(request.QuizId, cancellationToken);
                if (quiz == null)
                    return new StartQuizAttemptResponse { Success = false, Message = "Quiz not found" };

                if (!quiz.IsPublished)
                    return new StartQuizAttemptResponse { Success = false, Message = "Quiz not published" };

                // Check previous attempts
                var previousAttempts = await _attemptRepository.GetAllAsync(
                    a => a.QuizId == request.QuizId && a.UserId == request.UserId,
                    cancellationToken);

                var attemptCount = previousAttempts.Count();

                // Check max attempts
                if (quiz.MaxAttempts.HasValue && attemptCount >= quiz.MaxAttempts.Value)
                    return new StartQuizAttemptResponse
                    {
                        Success = false,
                        Message = $"Maximum attempts ({quiz.MaxAttempts}) reached"
                    };

                // Create new attempt
                var attempt = new QuizAttempt
                {
                    QuizId = quiz.Id,
                    UserId = request.UserId,
                    CourseId = quiz.CourseId,
                    AttemptNumber = attemptCount + 1,
                    Status = QuizAttemptStatus.InProgress,
                    StartedAt = DateTime.UtcNow,
                    ExpiresAt = quiz.TimeLimit.HasValue
                      ? DateTime.UtcNow.AddMinutes(quiz.TimeLimit.Value)
                      : null
                };

                await _attemptRepository.CreateAsync(attempt, cancellationToken);

                _logger.LogInformation(
                    "Quiz attempt started: {AttemptId} for Quiz {QuizId} by User {UserId}",
                    attempt.Id, quiz.Id, request.UserId);

                return new StartQuizAttemptResponse
                {
                    Success = true,
                    Message = "Quiz attempt started",
                    AttemptId = attempt.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting quiz attempt");
                return new StartQuizAttemptResponse { Success = false, Message = "Error starting quiz" };
            }
        }
    }

}

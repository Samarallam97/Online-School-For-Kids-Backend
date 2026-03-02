using Domain.Entities.Content.Quiz;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Course
{
    public class SubmitQuizCommand : IRequest<QuizResultDto?>
    {
        public string UserId { get; set; } = string.Empty;
        public SubmitQuizDto SubmitQuizDto { get; set; } = new();
    }

    public class SubmitQuizHandler : IRequestHandler<SubmitQuizCommand, QuizResultDto?>
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IAttemptRepository _attemptRepository;
        private readonly ILogger<SubmitQuizHandler> _logger;

        public SubmitQuizHandler(
            IQuizRepository quizRepository,
            IAttemptRepository attemptRepository,
            ILogger<SubmitQuizHandler> logger)
        {
            _quizRepository = quizRepository;
            _attemptRepository = attemptRepository;
            _logger = logger;
        }

        public async Task<QuizResultDto?> Handle(
            SubmitQuizCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Step 1:Retrieve Quiz and Attempt: Fetches the quiz details and the user's latest "In-Progress" attempt.

                // Get quiz
                var quiz = await _quizRepository.GetByIdAsync(request.SubmitQuizDto.QuizId, cancellationToken);
                if (quiz == null) return null;

                // Get latest in-progress attempt
                var attempts = await _attemptRepository.GetAllAsync(
                    a => a.QuizId == quiz.Id &&
                         a.UserId == request.UserId &&
                         a.Status == QuizAttemptStatus.InProgress,
                    cancellationToken);

                var attempt = attempts.OrderByDescending(a => a.StartedAt).FirstOrDefault();
                if (attempt == null) return null;

                if (attempt.ExpiresAt.HasValue && DateTime.UtcNow > attempt.ExpiresAt.Value)
                {
                    attempt.Status = QuizAttemptStatus.Expired;
                    attempt.CompletedAt = DateTime.UtcNow;                   
                    await _attemptRepository.UpdateAsync(attempt.Id, attempt, cancellationToken);

                    _logger.LogWarning("Quiz attempt expired: {AttemptId}", attempt.Id);

                    return new QuizResultDto
                    {
                        AttemptId = attempt.Id,
                        Score = 0,
                        TotalPoints = 0,
                        EarnedPoints = 0,
                        Passed = false,
                        TimeSpent = 0,
                        AttemptNumber = attempt.AttemptNumber
                    };
                }
                // Calculate score
                int totalPoints = quiz.Questions.Sum(q => q.Points);
                int earnedPoints = 0;

                var answerResults = new List<QuizAnswerResultDto>();
                // Step 2 :Iterate and Compare: Loops through each submitted answer to compare the selected option with the correct one.
                foreach (var answer in request.SubmitQuizDto.Answers)
                {
                    var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question == null) continue;

                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId);
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                    // Step 3:Calculate Scores: Sums the earned points and calculates the final percentage.
                    bool isCorrect = selectedOption?.IsCorrect ?? false;
                    int pointsEarned = isCorrect ? question.Points : 0;

                    earnedPoints += pointsEarned;
                    // Step 4 :Record Answers: Adds each answer detail (Selected Option, Correctness, Points) to the attempt record.

                    // Add to attempt answers
                    attempt.Answers.Add(new QuizAnswer
                    {
                        QuestionId = question.Id,
                        SelectedOptionId = answer.SelectedOptionId,
                        IsCorrect = isCorrect,
                        PointsEarned = pointsEarned,
                        AnsweredAt = DateTime.UtcNow
                    });
                    // Step 5:Prepare Review List: Generates a list of questions with both selected and correct answers for display.
                    // Add to result (if showAnswers enabled)
                    if (quiz.ShowAnswers)
                    {
                        answerResults.Add(new QuizAnswerResultDto
                        {
                            QuestionId = question.Id,
                            QuestionText = question.Text,
                            SelectedOptionId = answer.SelectedOptionId,
                            SelectedOptionText = selectedOption?.Text ?? "",
                            CorrectOptionId = correctOption?.Id ?? "",
                            CorrectOptionText = correctOption?.Text ?? "",
                            IsCorrect = isCorrect,
                            PointsEarned = pointsEarned,
                            Explanation = question.Explanation
                        });
                    }
                }
                //Step 6:Update Status: Marks the attempt as "Completed," sets the final score, and saves everything to the database.

                // Update attempt
                decimal score = totalPoints > 0 ? (decimal)earnedPoints / totalPoints * 100 : 0;
                bool passed = score >= quiz.PassingScore;

                attempt.Status = QuizAttemptStatus.Completed;
                attempt.CompletedAt = DateTime.UtcNow;
                attempt.TimeSpent =
                        (int)(attempt.CompletedAt.Value - attempt.StartedAt).TotalSeconds;
                attempt.Score = score;
                attempt.TotalPoints = totalPoints;
                attempt.EarnedPoints = earnedPoints;
                attempt.Passed = passed;
                attempt.TimeSpent = request.SubmitQuizDto.TimeSpent;

                await _attemptRepository.UpdateAsync(attempt.Id, attempt, cancellationToken);

                _logger.LogInformation(
                    "Quiz submitted: {AttemptId}, Score: {Score}%, Passed: {Passed}",
                    attempt.Id, score, passed);

                return new QuizResultDto
                {
                    AttemptId = attempt.Id,
                    Score = score,
                    TotalPoints = totalPoints,
                    EarnedPoints = earnedPoints,
                    Passed = passed,
                    TimeSpent = request.SubmitQuizDto.TimeSpent,
                    AttemptNumber = attempt.AttemptNumber,
                    Answers = answerResults
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quiz");
                return null;
            }
        }
    }
    public class SubmitQuizDto
    {
        public string QuizId { get; set; } = string.Empty;
        public List<QuizAnswerDto> Answers { get; set; } = new();
        public int TimeSpent { get; set; } // Seconds
    }
    public class QuizAnswerDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string SelectedOptionId { get; set; } = string.Empty;
    }
    public class QuizResultDto
    {
        public string AttemptId { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public int TotalPoints { get; set; }
        public int EarnedPoints { get; set; }
        public bool Passed { get; set; }
        public int TimeSpent { get; set; }
        public int AttemptNumber { get; set; }
        public List<QuizAnswerResultDto> Answers { get; set; } = new();
    }
   
    public class QuizAnswerResultDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string SelectedOptionId { get; set; } = string.Empty;
        public string SelectedOptionText { get; set; } = string.Empty;
        public string CorrectOptionId { get; set; } = string.Empty;
        public string CorrectOptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public string? Explanation { get; set; }
    }
}



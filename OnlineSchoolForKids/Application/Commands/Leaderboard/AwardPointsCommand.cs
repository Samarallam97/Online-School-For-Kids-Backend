using Domain.Entities.Content.Leaderboard;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using Domain.Interfaces.Repositories.Users;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Leaderboard
{
    public class AwardPointsCommand : IRequest<bool>
    {
        public AwardPointsDto Dto { get; set; } = new();
    }

    public class AwardPointsHandler : IRequestHandler<AwardPointsCommand, bool>
    {
        private readonly IUserPointsRepository _userPointsRepo;
        private readonly IPointTransactionRepository _transactionRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMediator _mediator;
        private readonly ILogger<AwardPointsHandler> _logger;

        public AwardPointsHandler(
            IUserPointsRepository userPointsRepo,
            IPointTransactionRepository transactionRepo,
            IUserRepository userRepo,
            IMediator mediator,
            ILogger<AwardPointsHandler> logger)
        {
            _userPointsRepo = userPointsRepo;
            _transactionRepo = transactionRepo;
            _userRepo = userRepo;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(AwardPointsCommand request, CancellationToken ct)
        {
            try
            {
                var dto = request.Dto;
                var user = await _userRepo.GetByIdAsync(dto.UserId, ct);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", dto.UserId);
                    return false;
                }
                // Get or create user points
                var userPoints = await _userPointsRepo.GetOneAsync(
                    up => up.UserId == dto.UserId,
                    ct);

                if (userPoints == null)
                {
                    userPoints = new UserPoints
                    {
                        UserId = dto.UserId,
                        UserName = user.FullName,
                        TotalPoints = dto.Points,
                        WeeklyPoints = dto.Points,
                        MonthlyPoints = dto.Points,                   
                        UserAvatar = user.ProfilePictureUrl,             
                        CoursesCompleted = dto.Reason == "CourseCompleted" ? 1 : 0, 
                    
                    };
                    await _userPointsRepo.CreateAsync(userPoints, ct);
                }
                else
                {
                    userPoints.TotalPoints += dto.Points;
                    userPoints.WeeklyPoints += dto.Points;
                    userPoints.MonthlyPoints += dto.Points;
                    if (dto.Reason == "CourseCompleted")
                    {
                        userPoints.CoursesCompleted++;
                    }

                    await _userPointsRepo.UpdateAsync(userPoints.Id, userPoints, ct);
                }

                // Record transaction
                var transaction = new PointTransaction
                {
                    UserId = dto.UserId,
                    Points = dto.Points,
                    Reason = Enum.Parse<PointReason>(dto.Reason),
                    Description = dto.Description ?? "",
                    RelatedEntityId = dto.RelatedEntityId
                };

                await _transactionRepo.CreateAsync(transaction, ct);
                var newBadges = await _mediator.Send(
                    new CheckAndAwardBadgesCommand { UserId = dto.UserId },
                    ct);

                if (newBadges.Any())
                {
                    _logger.LogInformation(
                        "User {UserId} earned {Count} new badges: {Badges}",
                        dto.UserId, newBadges.Count, string.Join(", ", newBadges));
                }
                _logger.LogInformation("Points awarded: {Points} to User {UserId} for {Reason}",
                    dto.Points, dto.UserId, dto.Reason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding points");
                return false;
            }
        }
    }
    public class AwardPointsDtoValidator : AbstractValidator<AwardPointsDto>
    {
        public AwardPointsDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Points)
                .GreaterThan(0).WithMessage("Points must be greater than 0")
                .LessThanOrEqualTo(10000).WithMessage("Points cannot exceed 10,000 per transaction");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .Must(reason => new[]
                {
                    "CourseCompleted",
                    "LessonCompleted",
                    "QuizPassed",
                    "StreakMaintained",
                    "BadgeEarned",
                    "DailyLogin",
                    "AssignmentSubmitted"
                }.Contains(reason))
                .WithMessage("Invalid reason. Must be CourseCompleted, LessonCompleted, QuizPassed, StreakMaintained, BadgeEarned, DailyLogin, or AssignmentSubmitted");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.RelatedEntityId)
                .MaximumLength(50).WithMessage("Related entity ID cannot exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.RelatedEntityId));
        }
    }


    public class AwardPointsDto
    {
        public string UserId { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RelatedEntityId { get; set; }
    }

}

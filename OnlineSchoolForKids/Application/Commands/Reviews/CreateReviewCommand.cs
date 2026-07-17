using Domain.Entities.Reviews;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;


namespace Application.Commands.Reviews;


public record CreateReviewCommand(string UserId, int Rating, string Comment) : IRequest<ReviewDto>;



    public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewDto>
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;

        public CreateReviewCommandHandler(IReviewRepository reviewRepository, IUserRepository userRepository)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
        }

        public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken ct)
        {
            var review = new Review
            {
                UserId = request.UserId,
                Rating = Math.Clamp(request.Rating, 1, 5),
                Comment = request.Comment,
                IsApproved = true,
            };

            // CreateAsync sets CreatedAt/UpdatedAt for you
            await _reviewRepository.CreateAsync(review, ct);

            var user = await _userRepository.GetByIdAsync(request.UserId, ct);

            return new ReviewDto(
                review.Id,
                review.UserId,
                user?.FullName ?? "Anonymous",
                user?.ProfilePictureUrl,
                review.Rating,
                review.Comment,
                review.CreatedAt);
        }
    }

public record ReviewDto(
        string Id,
        string UserId,
        string UserName,
        string? UserAvatarUrl,
        int Rating,
        string Comment,
        DateTime CreatedAt);

public class CreateReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
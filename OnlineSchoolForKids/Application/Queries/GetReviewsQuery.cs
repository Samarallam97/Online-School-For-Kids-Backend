using Application.Commands.Reviews;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Application.Queries;


public record GetReviewsQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<ReviewDto>>;

public class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, PagedResult<ReviewDto>>
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;

        public GetReviewsQueryHandler(IReviewRepository reviewRepository, IUserRepository userRepository)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
        }

        public async Task<PagedResult<ReviewDto>> Handle(GetReviewsQuery request, CancellationToken ct)
        {
            var skip = (request.Page - 1) * request.PageSize;

            var (items, totalCount) = await _reviewRepository.GetApprovedPagedAsync(skip, request.PageSize, ct);
            var itemsList = items.ToList();

            // Batch-fetch reviewers instead of one lookup per review
            var userIds = itemsList.Select(r => r.UserId).Distinct().ToList();
            var users = await _userRepository.GetManyByIdsAsync(userIds, ct);
            var userLookup = users.ToDictionary(u => u.Id, u => u);

            var dtos = itemsList.Select(r =>
            {
                userLookup.TryGetValue(r.UserId, out var user);
                return new ReviewDto(
                    r.Id, r.UserId, user?.FullName ?? "Anonymous", user?.ProfilePictureUrl,
                    r.Rating, r.Comment, r.CreatedAt);
            }).ToList();

            return new PagedResult<ReviewDto>
            {
                Items = dtos,
                TotalCount = (int)totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }

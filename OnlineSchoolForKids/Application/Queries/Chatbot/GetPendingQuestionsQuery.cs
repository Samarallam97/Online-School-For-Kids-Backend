using Application.DTOs;
using Domain.Entities.Chatbot;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Queries.Chatbot
{
    public record PendingQuestionDto(
    string Id,
    string? UserId,
    string Question,
    string Language,
    double Similarity,
    string Status,
    string? Answer,
    bool PushedToChatbot,
    DateTime CreatedAt,
    DateTime? AnsweredAt);

    public record GetPendingQuestionsQuery(
        string? Status,  // "Pending" | "Answered" | null = all
        int Page = 1,
        int PageSize = 20)
        : IRequest<Result<(IEnumerable<PendingQuestionDto> Items, long TotalCount)>>;

    public class GetPendingQuestionsQueryHandler
        : IRequestHandler<GetPendingQuestionsQuery,
            Result<(IEnumerable<PendingQuestionDto> Items, long TotalCount)>>
    {
        private readonly IPendingQuestionRepository _repo;

        public GetPendingQuestionsQueryHandler(IPendingQuestionRepository repo) => _repo = repo;

        public async Task<Result<(IEnumerable<PendingQuestionDto> Items, long TotalCount)>> Handle(
            GetPendingQuestionsQuery request, CancellationToken ct)
        {
            PendingQuestionStatus? statusFilter = request.Status?.ToLower() switch
            {
                "pending" => PendingQuestionStatus.Pending,
                "answered" => PendingQuestionStatus.Answered,
                _ => null
            };

            var pageSize = Math.Clamp(request.PageSize, 1, 50);
            var skip = (Math.Max(request.Page, 1) - 1) * pageSize;

            var (items, total) = await _repo.GetPagedAsync(statusFilter, skip, pageSize, ct);

            var dtos = items.Select(q => new PendingQuestionDto(
                q.Id, q.UserId, q.Question, q.Language,
                q.Similarity, q.Status.ToString(),
                q.Answer, q.PushedToChatbot,
                q.CreatedAt, q.AnsweredAt));

            return Result<(IEnumerable<PendingQuestionDto>, long)>.Success((dtos, total));
        }
    }

}

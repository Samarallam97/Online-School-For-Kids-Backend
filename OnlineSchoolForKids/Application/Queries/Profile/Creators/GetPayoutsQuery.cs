using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;



namespace Application.Queries.Profile.Creators;


public class GetPayoutsQuery : IRequest<PayoutsResponseDto>
{
    public string CreatorId { get; set; } = string.Empty;
    public PayoutStatus? Status { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public class GetPayoutsQueryHandler : IRequestHandler<GetPayoutsQuery, PayoutsResponseDto>
{
    private readonly IPayoutRepository _payoutRepository;

    public GetPayoutsQueryHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<PayoutsResponseDto> Handle(GetPayoutsQuery request, CancellationToken cancellationToken)
    {
        // Get paginated payouts
        var (payouts, totalCount) = await _payoutRepository.GetByCreatorIdPagedAsync(
            request.CreatorId,
            request.Status,
            request.Offset,
            request.Limit,
            cancellationToken
        );

        // Get next payout
        var nextPayout = await _payoutRepository.GetNextPayoutAsync(request.CreatorId, cancellationToken);

        return new PayoutsResponseDto
        {
            Payouts = payouts.Select(MapToDto).ToList(),
            Total = (int)totalCount,
            NextPayout = nextPayout != null ? MapToDto(nextPayout) : null
        };
    }

    private static PayoutDto MapToDto(Payout payout)
    {
        return new PayoutDto
        {
            Id = payout.Id,
            Amount = payout.Amount,
            Status = payout.Status.ToString().ToLower(),
            PaymentMethodId = payout.PaymentMethodId,
            ScheduledDate = payout.ScheduledDate.ToString("yyyy-MM-dd"),
            ProcessedDate = payout.ProcessedDate?.ToString("yyyy-MM-dd"),
            Currency = payout.Currency,
            Month = payout.Month,
            Year = payout.Year
        };
    }
}

public class PayoutsResponseDto
{
    public List<PayoutDto> Payouts { get; set; } = new();
    public int Total { get; set; }
    public PayoutDto? NextPayout { get; set; }
}

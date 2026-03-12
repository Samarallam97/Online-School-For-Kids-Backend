using Domain.Interfaces.Repositories.Content;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Content;

public class GetWishlistCountQuery : IRequest<GetWishlistCountResponse>
{
    public string UserId { get; set; } = string.Empty;
}

public class GetWishlistCountResponse
{
    public int Count { get; set; }
    public bool Success { get; set; }
}

public class GetWishlistCountQueryHandler
    : IRequestHandler<GetWishlistCountQuery, GetWishlistCountResponse>
{
    private readonly IWishListRepository _wishRepo;

    public GetWishlistCountQueryHandler(IWishListRepository wishRepo)
    {
        _wishRepo = wishRepo;
    }

    public async Task<GetWishlistCountResponse> Handle(
        GetWishlistCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _wishRepo.CountAsync(
            w => w.UserId == request.UserId,
            cancellationToken);

        return new GetWishlistCountResponse
        {
            Count   = (int)count,
            Success = true,
        };
    }
}
using Domain.Interfaces.Repositories.Content;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Course;

public class ClearWishlistCommand : IRequest<ClearWishlistResponse>
{
    public string UserId { get; set; } = string.Empty;
}

public class ClearWishlistResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DeletedCount { get; set; }
}

public class ClearWishlistCommandHandler
    : IRequestHandler<ClearWishlistCommand, ClearWishlistResponse>
{
    private readonly IWishListRepository _wishRepo;

    public ClearWishlistCommandHandler(IWishListRepository wishRepo)
    {
        _wishRepo = wishRepo;
    }

    public async Task<ClearWishlistResponse> Handle(
        ClearWishlistCommand request,
        CancellationToken cancellationToken)
    {
        var entries = await _wishRepo.GetAllAsync(
            w => w.UserId == request.UserId,
            cancellationToken);

        var list = entries.ToList();

        if (list.Count == 0)
            return new ClearWishlistResponse
            {
                Success      = true,
                Message      = "Wishlist is already empty",
                DeletedCount = 0,
            };

        // Soft-delete all entries in parallel
        await Task.WhenAll(list.Select(w => _wishRepo.DeleteAsync(w.Id, cancellationToken)));

        return new ClearWishlistResponse
        {
            Success      = true,
            Message      = $"{list.Count} course(s) removed from wishlist",
            DeletedCount = list.Count,
        };
    }
}


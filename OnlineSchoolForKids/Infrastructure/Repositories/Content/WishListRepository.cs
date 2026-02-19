using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories.Content;

public class WishListRepository : GenericRepository<Wishlist> , IWishListRepository
{
    public WishListRepository(MongoDbContext context): base(context.Wishlists)
    {
        
    }
}

using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
namespace Application.Commands
{
    public class DeleteFromFavouriteCommandHandler : IRequestHandler<DeleteFromFavouriteCommand, DeleteFromFavouriteResponse>
    {
        
        private readonly IGenericRepository<Wishlist> _wishRepo;
        

        public DeleteFromFavouriteCommandHandler(
             IGenericRepository<Wishlist> wishRepo)
        {       
            _wishRepo = wishRepo;
        }

        public async Task<DeleteFromFavouriteResponse> Handle(DeleteFromFavouriteCommand request, CancellationToken cancellationToken)
        {
            
                // Find favourite entry
                var favourite = (await _wishRepo.FindAsync(w =>
                    w.UserId == request.UserId && w.CourseId == request.CourseId))
                    .FirstOrDefault();

                if (favourite == null)
                {
                    return new DeleteFromFavouriteResponse
                    {
                        Success = false,
                        Message = "Course not found in favourites"
                    };
                }

                // Delete from favourites
                await _wishRepo.DeleteAsync(favourite.Id);
                return new DeleteFromFavouriteResponse
                {
                   Success = true,
                   Message = "Course deleted from favourites successfully"
                };


        }
           
            
        }
    }


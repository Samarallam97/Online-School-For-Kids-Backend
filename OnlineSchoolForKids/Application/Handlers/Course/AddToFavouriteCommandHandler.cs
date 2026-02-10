using Application.Commands;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using MongoDB.Bson;
using CourseEntity = Domain.Entities.Course;

namespace Application.Handlers.Course
{
    public class AddToFavouriteCommandHandler : IRequestHandler<AddToFavouriteCommand, AddToFavouriteResponse>
    {
        
        
        private readonly IGenericRepository<CourseEntity> _courseRepo;
        private readonly IGenericRepository<Wishlist> _wishRepo;

        public AddToFavouriteCommandHandler(         
            IGenericRepository<CourseEntity> courseRepo,IGenericRepository<Wishlist> wishRepo)
        {
          
         
            _courseRepo = courseRepo;
            _wishRepo = wishRepo;
        }

        public async Task<AddToFavouriteResponse> Handle(AddToFavouriteCommand request, CancellationToken cancellationToken)
        {

            // Check if course exists and is published
            var course = await _courseRepo.GetByIdAsync(request.CourseId);

            if (course == null)
            {
                return new AddToFavouriteResponse
                {
                    Success = false,
                    Message = "Course not found"
                };
            }

            if (!course.IsPublished)
            {
                return new AddToFavouriteResponse
                {
                    Success = false,
                    Message = "Course is not available"
                };
            }

            // Check if already in favourites
            var existingFavourite = await _wishRepo.FindAsync(w =>
                w.UserId == request.UserId && w.CourseId == request.CourseId);

            if (existingFavourite.Any())
            {
                return new AddToFavouriteResponse
                {
                    Success = false,
                    Message = "Course already in favourites",
                    FavouriteId = existingFavourite.First().Id
                };
            }

            // Create new favourite entry
            var favourite = new Wishlist
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = request.UserId,
                CourseId = request.CourseId,
                CreatedAt = DateTime.UtcNow
            };

            await _wishRepo.CreateAsync(favourite);

            return new AddToFavouriteResponse
            {
                Success = true,
                Message = "Course added to favourites successfully",
                FavouriteId = favourite.Id
            };
        }
    }
        }
 
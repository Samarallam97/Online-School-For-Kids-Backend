using Application.Dtos;
using Application.Queries;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;
using CourseEntity = Domain.Entities.Course;
namespace Application.Handlers.Course
{
    public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseDto>
    {
        private readonly IGenericRepository<CourseEntity> _courseRepo;
        private readonly IMapper _mapper;

        public GetCourseByIdQueryHandler(IGenericRepository<CourseEntity> courseRepo, IMapper mapper)
        {
            _courseRepo = courseRepo;
            _mapper = mapper;

        }
        public async Task<CourseDto?> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
        {
            // Get course by ID
            var course = await _courseRepo.GetByIdAsync(request.CourseId);

            // Return null if course not found or not published
            if (course == null || !course.IsPublished)
                return null;

            // Get wishlist and cart status if user is logged in
            var isInWishlist = false;
            var isInCart = false;

            //if (!string.IsNullOrEmpty(request.UserId) 
            //{
            //    // Check if course is in user's wishlist
            //    var wishlist = await _unitOfWork.Wishlists.FindAsync(w =>
            //        w.UserId == userId && w.CourseId == request.CourseId);
            //    isInWishlist = wishlist.Any();

            //    // Check if course is in user's cart
            //    var cartItem = await _unitOfWork.CartItems.FindAsync(c =>
            //        c.UserId == userId && c.CourseId == request.CourseId);
            //    isInCart = cartItem.Any();
            //}

            // Map to DTO using AutoMapper
            var courseDto = _mapper.Map<CourseDto>(course);

            // Set wishlist and cart status
            courseDto.IsInWishlist = isInWishlist;
            courseDto.IsInCart = isInCart;

            return courseDto;
        }
    }
}





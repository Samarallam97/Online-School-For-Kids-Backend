using Domain.Entities.Content;
using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;
using MongoDB.Bson;

namespace Application.Commands
{
    public class AddToFavouriteCommand : IRequest<AddToFavouriteResponse>
    {
        public string CourseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class AddToFavouriteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FavouriteId { get; set; }
    }
    public class AddToFavouriteCommandHandler : IRequestHandler<AddToFavouriteCommand, AddToFavouriteResponse>
    {


        private readonly ICourseRepository _courseRepo;
        private readonly IWishListRepository _wishRepo;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AddToFavouriteCommandHandler(
            ICourseRepository courseRepo, IWishListRepository wishRepo, IStringLocalizer<SharedResource> localizer)
        {


            _courseRepo = courseRepo;
            _wishRepo = wishRepo;
            _localizer = localizer;
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
                    Message = _localizer["CourseIsNotFound"]
                };
            }

            if (!course.IsPublished)
            {
                return new AddToFavouriteResponse
                {
                    Success = false,
                    Message = _localizer["CourseIsNotAvailable"]
                };
            }

            // Check if already in favourites
            var existingFavourite = await _wishRepo.GetAllAsync(w =>
                w.UserId == request.UserId && w.CourseId == request.CourseId);

            if (existingFavourite.Any())
            {
                return new AddToFavouriteResponse
                {
                    Success = false,
                    Message = _localizer["CourseAlreadyInFavourites"],
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
                Message = _localizer["CourseAddedToFavouritesSuccessfully"],
                FavouriteId = favourite.Id
            };
        }
        public class AddToFavouriteCommandValidator : AbstractValidator<AddToFavouriteCommand>
        {
            public AddToFavouriteCommandValidator(IStringLocalizer<SharedResource> localizer)
            {
                RuleFor(x => x.CourseId)
                    .NotEmpty()
                    .WithMessage(localizer["CourseIDIsRequired"]);

                RuleFor(x => x.UserId)
                    .NotEmpty()
                    .WithMessage(localizer["UserIdIsRequired"]);
            }
        }

    }
    public class DeleteFromFavouriteCommand : IRequest<DeleteFromFavouriteResponse>
    {
        public string CourseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class DeleteFromFavouriteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    public class DeleteFromFavouriteCommandHandler : IRequestHandler<DeleteFromFavouriteCommand, DeleteFromFavouriteResponse>
    {

        private readonly IWishListRepository _wishRepo;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public DeleteFromFavouriteCommandHandler(
             IWishListRepository wishRepo, IStringLocalizer<SharedResource> localizer)
        {
            _wishRepo = wishRepo;
            _localizer = localizer;
        }

        public async Task<DeleteFromFavouriteResponse> Handle(DeleteFromFavouriteCommand request, CancellationToken cancellationToken)
        {

            // Find favourite entry
            var favourite = (await _wishRepo.GetAllAsync(w =>
                w.UserId == request.UserId && w.CourseId == request.CourseId))
                .FirstOrDefault();

            if (favourite == null)
            {
                return new DeleteFromFavouriteResponse
                {
                    Success = false,
                    Message = _localizer["CourseNotFoundInFavourites"]
                };
            }

            // Delete from favourites
            await _wishRepo.DeleteAsync(favourite.Id);
            return new DeleteFromFavouriteResponse
            {
                Success = true,
                Message = _localizer["CourseDeletedFromFavouritesSuccessfully"]
            };


        }
        public class DeleteFromFavouriteCommandValidator : AbstractValidator<DeleteFromFavouriteCommand>
        {
            public DeleteFromFavouriteCommandValidator(IStringLocalizer<SharedResource> localizer)
            {
                RuleFor(x => x.CourseId)
                    .NotEmpty()
                    .WithMessage(localizer["CourseIDIsRequired"]);

                RuleFor(x => x.UserId)
                    .NotEmpty()
                    .WithMessage(localizer["UserIdIsRequired"]);
            }
        }
        public class AddToFavouriteRequest
        {
            public string CourseId { get; set; } = string.Empty;
        }

    }
}




using FluentValidation;

namespace Application.Commands
{
    public class AddToFavouriteCommandValidator : AbstractValidator<AddToFavouriteCommand>
    {
        public AddToFavouriteCommandValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty()
                .WithMessage("Course ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");
        } }
        public class DeleteFromFavouriteCommandValidator : AbstractValidator<DeleteFromFavouriteCommand>
        {
            public DeleteFromFavouriteCommandValidator()
            {
                RuleFor(x => x.CourseId)
                    .NotEmpty()
                    .WithMessage("Course ID is required");

                RuleFor(x => x.UserId)
                    .NotEmpty()
                    .WithMessage("User ID is required");
            }
        }
    }


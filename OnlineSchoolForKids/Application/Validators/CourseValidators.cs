using Application.Queries;
using FluentValidation;

namespace Application.Validators
{
    public class GetCourseByIdQueryValidator : AbstractValidator<GetCourseByIdQuery>
    {
        public GetCourseByIdQueryValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty()
                .WithMessage("Course ID is required");
        }
    }
}
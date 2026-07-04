using Domain.Interfaces.Repositories.Content;
using FluentValidation;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;
namespace Application.Commands.Course
{
    public class RemoveFromCartCommand : IRequest<RemoveFromCartResponse>
    {
        public string CourseId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
    public class RemoveFromCartResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    public class ClearCartCommand : IRequest<ClearCartResponse>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class ClearCartResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ItemsRemoved { get; set; }
    }

    public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, RemoveFromCartResponse>
    {
        private readonly ICartItemRepository _cartItemRepo;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RemoveFromCartCommandHandler(ICartItemRepository cartItemRepo, IStringLocalizer<SharedResource> localizer)
        {
            _cartItemRepo = cartItemRepo;
            _localizer = localizer;
        }
        public async Task<RemoveFromCartResponse> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
        {
            // Find cart item
            var cartItem = await _cartItemRepo.GetByUserAndCourseAsync(request.UserId, request.CourseId);

            if (cartItem == null)
            {
                return new RemoveFromCartResponse
                {
                    Success = false,
                    Message = _localizer["CourseNotFoundInCart"]
                };
            }
            // Remove from cart
            await _cartItemRepo.DeleteAsync(cartItem.Id);
            return new RemoveFromCartResponse
            {
                Success = true,
                Message = _localizer["CourseRemovedFromCartSuccessfully"]
            };
        }
        public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, ClearCartResponse>
        {
            private readonly ICartItemRepository _cartItemRepo;
            private readonly IStringLocalizer<SharedResource> _localizer;

            public ClearCartCommandHandler(
                ICartItemRepository cartItemRepo, IStringLocalizer<SharedResource> localizer)
            {
                _cartItemRepo = cartItemRepo;
                _localizer = localizer;
            }
            public async Task<ClearCartResponse> Handle(ClearCartCommand request, CancellationToken cancellationToken)
            {
                // Get all cart items for user
                var cartItems = await _cartItemRepo.GetUserCartItemsAsync(request.UserId);

                if (!cartItems.Any())
                {
                    return new ClearCartResponse
                    {
                        Success = true,
                        Message = _localizer["CartIsAlreadyEmpty"],
                        ItemsRemoved = 0
                    };
                }

                var itemCount = cartItems.Count();

                // Remove all cart items
                foreach (var item in cartItems)
                {
                    await _cartItemRepo.DeleteAsync(item.Id);
                }
                return new ClearCartResponse
                {
                    Success = true,
                    Message = $"Cart cleared successfully - {itemCount} item(s) removed",
                    ItemsRemoved = itemCount
                };
            }

            public class RemoveFromCartCommandValidator : AbstractValidator<RemoveFromCartCommand>
            {
                public RemoveFromCartCommandValidator(IStringLocalizer<SharedResource> localizer)
                {
                    RuleFor(x => x.CourseId)
                        .NotEmpty()
                        .WithMessage(localizer["CourseIDIsRequired"]);

                    RuleFor(x => x.UserId)
                        .NotEmpty()
                        .WithMessage(localizer["UserIdIsRequired"]);
                }
            }
            public class ClearCartCommandValidator : AbstractValidator<ClearCartCommand>
            {
                public ClearCartCommandValidator(IStringLocalizer<SharedResource> localizer)
                {
                    RuleFor(x => x.UserId)
                        .NotEmpty()
                        .WithMessage(localizer["UserIdIsRequired"]);
                }
            }
        }
    }
}


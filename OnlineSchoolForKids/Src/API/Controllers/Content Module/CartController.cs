using Application.Commands.Course;
using Application.Queries.Content;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class CartController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CartController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CartController(IMediator mediator, ILogger<CartController> logger, IStringLocalizer<SharedResource> localizer)
        {
            _mediator = mediator;
            _logger = logger;
            _localizer = localizer;
        }
        [HttpPost]
        [ProducesResponseType(typeof(AddToCartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AddToCartResponse>> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });
                }

                var command = new AddToCartCommand
                {
                    CourseId = dto.CourseId,
                    UserId = userId
                };
                var result = await _mediator.Send(command);
                if (!result.Success)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(new { message = result.Message, success = false });

                    return BadRequest(new { message = result.Message, success = false });
                }
                return Ok(new
                {
                    data = result,
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding course to cart");
                return StatusCode(500, new
                {
                    message = _localizer["ErrorAddingToCart"],
                    success = false
                });
            }
        }
        [HttpDelete("{courseId}")]
        [ProducesResponseType(typeof(RemoveFromCartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RemoveFromCartResponse>> RemoveFromCart(string courseId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });
                }
                var command = new RemoveFromCartCommand
                {
                    CourseId = courseId,
                    UserId = userId
                };

                var result = await _mediator.Send(command);

                if (!result.Success)
                {
                    return NotFound(new { message = result.Message, success = false });
                }

                return Ok(new
                {
                    message = result.Message,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing course from cart");
                return StatusCode(500, new
                {
                    message = _localizer["ErrorRemovingFromCart"],
                    success = false
                });
            }
        }
        [HttpDelete]
        [ProducesResponseType(typeof(ClearCartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ClearCartResponse>> ClearCart()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });
                }

                var command = new ClearCartCommand { UserId = userId };
                var result = await _mediator.Send(command);
                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return StatusCode(500, new
                {
                    message = _localizer["ErrorClearingCart"],
                    success = false
                });
            }
        }
        [HttpGet]
        [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });
                }
                var query = new GetCartQuery { UserId = userId };
                var result = await _mediator.Send(query);
                return Ok(new
                {
                    data = result,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart");
                return StatusCode(500, new
                {
                    message = _localizer["ErrorRetrievingCart"],
                    success = false
                });
            }
        }



        [HttpGet("count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCartCount()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (String.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });
                }
                var cart = await _mediator.Send(new GetCartQuery { UserId = userId });
                return Ok(new
                {
                    data = new
                    {
                        count = cart.ItemCount,
                        total = cart.Total
                    },
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return StatusCode(500, new
                {
                    message = _localizer["AnErrorOccurred"],
                    success = false
                });
            }
        }
        public class AddToCartDto
        {
            public string CourseId { get; set; } = string.Empty;
        }
    }

}
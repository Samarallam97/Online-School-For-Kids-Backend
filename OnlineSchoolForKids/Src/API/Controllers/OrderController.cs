using Application.Commands.Order;
using Application.DTOs;
using Application.Queries;
using Application.Queries.GetUserOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IMediator mediator, ILogger<OrderController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


        [HttpPost]
        public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new CreateOrderCommand
                {
                    UserId = userId,
                    PaymentMethod = dto.PaymentMethod,
                    Notes = dto.Notes
                };

                var result = await _mediator.Send(command);

                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new { data = result, message = result.Message, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { message = "An error occurred while creating order", success = false });
            }
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetOrders()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetUserOrdersQuery { UserId = userId };
                var result = await _mediator.Send(query);

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var query = new GetOrderQuery { OrderId = id, UserId = userId };
                var result = await _mediator.Send(query);

                if (result == null)
                    return NotFound(new { message = "Order not found", success = false });

                return Ok(new { data = result, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }


        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<CancelOrderResponse>> CancelOrder(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new CancelOrderCommand { OrderId = id, UserId = userId };
                var result = await _mediator.Send(command);

                if (!result.Success)
                    return BadRequest(new { message = result.Message, success = false });

                return Ok(new { message = result.Message, success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
    }
}



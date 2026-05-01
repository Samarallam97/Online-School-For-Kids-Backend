using Application.Commands.Moderation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace API.Controllers.Content_Module
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IMediator mediator, ILogger<ReportController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpPost("content")]
        public async Task<IActionResult> ReportContent(
            [FromBody] ReportContentDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null )
                    return Unauthorized(new { message = "User not authenticated", success = false });

                var command = new ReportContentCommand
                {
                    UserId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = "Already reported or invalid content", success = false });

                return Ok(new { message = "Content reported successfully", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting content");
                return StatusCode(500, new { message = "An error occurred", success = false });
            }
        }
       
    }
}

 

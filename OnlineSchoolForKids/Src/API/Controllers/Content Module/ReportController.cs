using Application.Commands.Moderation;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ReportController(IMediator mediator, ILogger<ReportController> logger, IStringLocalizer<SharedResource> localizer)
        {
            _mediator = mediator;
            _logger = logger;
            _localizer = localizer;
        }
        [HttpPost("content")]
        public async Task<IActionResult> ReportContent(
            [FromBody] ReportContentDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new { message = _localizer["UserNotAuthenticated"], success = false });

                var command = new ReportContentCommand
                {
                    UserId = userId,
                    Dto = dto
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                    return BadRequest(new { message = _localizer["AlreadyReportedOrInvalidContent"], success = false });

                return Ok(new { message = _localizer["ContentReportedSuccessfully"], success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting content");
                return StatusCode(500, new { message = _localizer["AnErrorOccurred"], success = false });
            }
        }

    }
}



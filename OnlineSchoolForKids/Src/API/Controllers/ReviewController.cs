using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Application.Commands.Reviews;
using Application.Queries;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public ReviewController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews([FromQuery] GetReviewsQuery query, CancellationToken ct)
    {
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateReviewCommand(UserId, dto.Rating, dto.Comment), ct);
        return Ok(result);
    }
}
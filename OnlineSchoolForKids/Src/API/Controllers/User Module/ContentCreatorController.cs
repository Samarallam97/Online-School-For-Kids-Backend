using Application.Commands.Profile;
using Application.Commands.Profile.Creator;
using Application.DTOs.Profile;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

using Application.Queries.Profile.Creators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContentCreatorController : ControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public ContentCreatorController(IMediator mediator) => _mediator = mediator;

    [HttpGet("courses")]
    public async Task<IActionResult> GetCreatorCourses(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCreatorCoursesQuery(UserId), ct);
        return Ok(result);
    }

    [HttpPut("courses/{courseId}/profile-visibility")]
    public async Task<IActionResult> ToggleCourseVisibility(string courseId, [FromBody] ToggleCourseVisibilityDto dto, CancellationToken ct)
    {
        await _mediator.Send(new ToggleCourseVisibilityCommand(UserId, courseId, dto.IsPublishedOnProfile), ct);
        return NoContent();
    }
}

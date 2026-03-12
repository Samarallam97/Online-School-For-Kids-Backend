using Application.Queries.Content;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Content_Module;

[Route("api/[controller]")]
[ApiController]
public sealed class CategoryController : ControllerBase
{
    private readonly ISender _sender;

    public CategoryController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(
            new GetCategoriesQuery(search, page, pageSize), ct);

        return Ok(new { success = true, data = result });
    }
}
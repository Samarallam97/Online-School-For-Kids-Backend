using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]

public class FeedController : ControllerBase
{
    private readonly IFeedService _feed;
    public FeedController(IFeedService feed) => _feed = feed;

    private string? Me => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // GET /api/feed?page=1&pageSize=10
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeed(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => Ok(await _feed.GetFeedAsync(page, pageSize, Me, ct));

    // POST /api/feed
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreatePostRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { message = "Content is required." });
        var post = await _feed.CreatePostAsync(Me!, req, ct);
        return CreatedAtAction(nameof(GetFeed), new { }, post);
    }

    // DELETE /api/feed/{postId}
    [HttpDelete("{postId}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(string postId, CancellationToken ct = default)
    {
        var ok = await _feed.DeletePostAsync(postId, Me!, ct);
        return ok ? NoContent() : NotFound(new { message = "Post not found or not yours." });
    }

    // POST /api/feed/{postId}/react
    [HttpPost("{postId}/react")]
    [Authorize]
    public async Task<IActionResult> React(
        string postId, [FromBody] ReactRequest req, CancellationToken ct = default)
    {
        try { return Ok(await _feed.ReactAsync(postId, Me!, req.ReactionType, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // GET /api/feed/{postId}/comments
    [HttpGet("{postId}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(string postId, CancellationToken ct = default)
        => Ok(await _feed.GetCommentsAsync(postId, ct));

    // POST /api/feed/{postId}/comments
    [HttpPost("{postId}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(
        string postId, [FromBody] CreateCommentRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { message = "Content is required." });
        try { return Ok(await _feed.AddCommentAsync(postId, Me!, req, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /api/feed/{postId}/share
    [HttpPost("{postId}/share")]
    [AllowAnonymous]
    public async Task<IActionResult> Share(string postId, CancellationToken ct = default)
    {
        var ok = await _feed.SharePostAsync(postId, ct);
        return ok ? Ok(new { message = "Tracked." }) : NotFound();
    }

    // ── Follow ────────────────────────────────────────────────────────────────

    // POST /api/feed/follow/{userId}  — toggle follow/unfollow
    [HttpPost("follow/{userId}")]
    [Authorize]
    public async Task<IActionResult> ToggleFollow(string userId, CancellationToken ct = default)
    {
        try { return Ok(await _feed.ToggleFollowAsync(Me!, userId, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // GET /api/feed/follow-stats/{userId}  — followers / following counts + am I following?
    [HttpGet("follow-stats/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowStats(string userId, CancellationToken ct = default)
        => Ok(await _feed.GetFollowStatsAsync(userId, Me, ct));

    // GET /api/feed/user/{userId}/posts?page=1&pageSize=10  — posts on a profile page
    [HttpGet("user/{userId}/posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserPosts(
        string userId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => Ok(await _feed.GetUserPostsAsync(userId, Me, page, pageSize, ct));
}

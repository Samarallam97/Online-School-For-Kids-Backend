using Domain.Interfaces.Services.Shared;
using Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private static readonly string[] AllowedImageExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedVideoExts = { ".mp4", ".mov", ".webm", ".avi" };
    private const long MaxImageSize = 10 * 1024 * 1024;  // 10 MB
    private const long MaxVideoSize = 100 * 1024 * 1024; // 100 MB

    public UploadController(IFileStorageService fileStorage, IStringLocalizer<SharedResource> localizer)
    {
        _fileStorage = fileStorage;
        _localizer = localizer;
    }

    /// <summary>POST /api/upload/feed-media — upload image or video for a feed post</summary>
    [HttpPost("feed-media")]
    public async Task<IActionResult> UploadFeedMedia(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = _localizer["NoFileProvided"] });

        var ext = _fileStorage.GetFileExtension(file.FileName);
        bool isImage = AllowedImageExts.Contains(ext);
        bool isVideo = AllowedVideoExts.Contains(ext);

        if (!isImage && !isVideo)
            return BadRequest(new { message = $"File type '{ext}' is not allowed." });

        if (isImage && file.Length > MaxImageSize)
            return BadRequest(new { message = _localizer["ImageSizeUnder10MB"] });

        if (isVideo && file.Length > MaxVideoSize)
            return BadRequest(new { message = _localizer["VideoSizeUnder100MB"] });

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadFileAsync(stream, file.FileName, "feed");

        return Ok(new
        {
            url,
            mediaType = isImage ? "image" : "video"
        });
    }


    /// <summary>POST /api/upload/chat-media — upload an image or file attachment for chat</summary>
    [HttpPost("chat-media")]
    public async Task<IActionResult> UploadChatMedia(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = _localizer["NoFileProvided"] });

        const long maxChatFileSize = 25 * 1024 * 1024; // 25 MB — adjust as you like
        if (file.Length > maxChatFileSize)
            return BadRequest(new { message = _localizer["FileSizeUnder25MB"] });

        var isImage = _fileStorage.IsImageFile(file.FileName);

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadFileAsync(stream, file.FileName, "chat");

        return Ok(new
        {
            url,
            fileName = file.FileName,
            type = isImage ? "image" : "file"
        });
    }
}
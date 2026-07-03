using Domain.Interfaces.Services.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private static readonly string[] AllowedImageExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedVideoExts = { ".mp4", ".mov", ".webm", ".avi" };
    private static readonly string[] AllowedMaterialExts = {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
        ".zip", ".rar", ".txt", ".jpg", ".jpeg", ".png",
    };
    private const long MaxImageSize = 10 * 1024 * 1024;  // 10 MB
    private const long MaxVideoSize = 100 * 1024 * 1024; // 100 MB
    private const long MaxThumbnailSize = 5 * 1024 * 1024;       // 5 MB
    private const long MaxPreviewVideoSize = 250 * 1024 * 1024; // 250 MB
    private const long MaxMaterialSize = 50 * 1024 * 1024;      // 50 MB

    public UploadController(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    /// <summary>POST /api/upload/feed-media — upload image or video for a feed post</summary>
    [HttpPost("feed-media")]
    public async Task<IActionResult> UploadFeedMedia(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });
        var ext = _fileStorage.GetFileExtension(file.FileName);
        bool isImage = AllowedImageExts.Contains(ext);
        bool isVideo = AllowedVideoExts.Contains(ext);
        if (!isImage && !isVideo)
            return BadRequest(new { message = $"File type '{ext}' is not allowed." });
        if (isImage && file.Length > MaxImageSize)
            return BadRequest(new { message = "Image must be under 10 MB." });
        if (isVideo && file.Length > MaxVideoSize)
            return BadRequest(new { message = "Video must be under 100 MB." });
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
            return BadRequest(new { message = "No file provided." });
        const long maxChatFileSize = 25 * 1024 * 1024; // 25 MB — adjust as you like
        if (file.Length > maxChatFileSize)
            return BadRequest(new { message = "File must be under 25 MB." });
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

    /// <summary>POST /api/upload/course-thumbnail — upload a course thumbnail image</summary>
    [HttpPost("course-thumbnail")]
    public async Task<IActionResult> UploadCourseThumbnail(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = _fileStorage.GetFileExtension(file.FileName);
        if (!AllowedImageExts.Contains(ext))
            return BadRequest(new { message = $"File type '{ext}' is not allowed for a thumbnail." });

        if (file.Length > MaxThumbnailSize)
            return BadRequest(new { message = "Thumbnail image must be under 5 MB." });

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadFileAsync(stream, file.FileName, "course-thumbnails");
        return Ok(new { url });
    }

    /// <summary>POST /api/upload/course-preview-video — upload a course's promotional preview clip</summary>
    [HttpPost("course-preview-video")]
    public async Task<IActionResult> UploadCoursePreviewVideo(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = _fileStorage.GetFileExtension(file.FileName);
        if (!AllowedVideoExts.Contains(ext))
            return BadRequest(new { message = $"File type '{ext}' is not allowed for a preview video." });

        if (file.Length > MaxPreviewVideoSize)
            return BadRequest(new { message = "Preview video must be under 250 MB." });

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadFileAsync(stream, file.FileName, "course-previews");
        return Ok(new { url });
    }

    /// <summary>POST /api/upload/course-material — upload a downloadable lesson resource</summary>
    [HttpPost("course-material")]
    public async Task<IActionResult> UploadCourseMaterial(
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = _fileStorage.GetFileExtension(file.FileName);
        if (!AllowedMaterialExts.Contains(ext))
            return BadRequest(new { message = $"File type '{ext}' is not allowed for a lesson material." });

        if (file.Length > MaxMaterialSize)
            return BadRequest(new { message = "Material file must be under 50 MB." });

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadFileAsync(stream, file.FileName, "course-materials");
        return Ok(new { url, fileName = file.FileName });
    }
}
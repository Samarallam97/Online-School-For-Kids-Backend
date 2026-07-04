using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;


public class UploadProfilePictureCommand : IRequest<UploadProfilePictureDto>
{
    public string UserId { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
}


public class UploadProfilePictureCommandHandler
    : IRequestHandler<UploadProfilePictureCommand, UploadProfilePictureDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public UploadProfilePictureCommandHandler(
        IUserRepository userRepository,
        IFileStorageService fileStorageService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _localizer = localizer;
    }

    public async Task<UploadProfilePictureDto> Handle(
        UploadProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);

        // Validate file
        if (request.File == null || request.File.Length == 0)
            throw new ArgumentException(_localizer["NoFileProvided"]);

        if (request.File.Length > MaxFileSize)
            throw new ArgumentException(_localizer["FileSizeExceeded"]);

        if (!_fileStorageService.IsImageFile(request.File.FileName))
            throw new ArgumentException(_localizer["OnlyImageFilesAllowed"]);

        // Delete old profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _fileStorageService.DeleteFileAsync(user.ProfilePictureUrl);
        }

        // Upload new profile picture
        using var stream = request.File.OpenReadStream();
        var fileUrl = await _fileStorageService.UploadFileAsync(
            stream,
            request.File.FileName,
            "profile-pictures"
        );

        // Update user profile
        user.ProfilePictureUrl = fileUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        return new UploadProfilePictureDto
        {
            ProfilePictureUrl = fileUrl
        };
    }
}

public class UploadProfilePictureDto
{
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string Message { get; set; } = "Profile picture updated successfully";
}

using Application.DTOs.Profile;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;


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
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public UploadProfilePictureCommandHandler(
        IUserRepository userRepository,
        IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<UploadProfilePictureDto> Handle(
        UploadProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Validate file
        if (request.File == null || request.File.Length == 0)
            throw new ArgumentException("No file provided");

        if (request.File.Length > MaxFileSize)
            throw new ArgumentException("File size exceeds maximum allowed size of 5MB");

        if (!_fileStorageService.IsImageFile(request.File.FileName))
            throw new ArgumentException("Only image files are allowed (jpg, jpeg, png, gif, webp)");

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
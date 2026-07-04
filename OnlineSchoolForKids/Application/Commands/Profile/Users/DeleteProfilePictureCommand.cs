using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Users;

public class DeleteProfilePictureCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
}

public class DeleteProfilePictureCommandHandler : IRequestHandler<DeleteProfilePictureCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteProfilePictureCommandHandler(
        IUserRepository userRepository,
        IFileStorageService fileStorageService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _localizer = localizer;
    }

    public async Task<Unit> Handle(DeleteProfilePictureCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException(_localizer["UserNotFound"]);

        // Delete profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            await _fileStorageService.DeleteFileAsync(user.ProfilePictureUrl);

            // Update user profile
            user.ProfilePictureUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
        }

        return Unit.Value;
    }
}
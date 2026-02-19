using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Users;

public class DeleteProfilePictureCommand : IRequest<Unit>
{
    public string UserId { get; set; } = string.Empty;
}

public class DeleteProfilePictureCommandHandler : IRequestHandler<DeleteProfilePictureCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteProfilePictureCommandHandler(
        IUserRepository userRepository,
        IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Unit> Handle(DeleteProfilePictureCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("User not found");

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
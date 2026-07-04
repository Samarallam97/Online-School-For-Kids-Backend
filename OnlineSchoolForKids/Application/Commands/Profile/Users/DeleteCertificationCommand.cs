using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Users;
public record DeleteCertificationCommand(string UserId, string CertificationId) : IRequest;

public class DeleteCertificationCommandHandler : IRequestHandler<DeleteCertificationCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteCertificationCommandHandler(IUserRepository userRepository, IFileStorageService fileStorageService, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _localizer = localizer;
    }

    public async Task Handle(DeleteCertificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["UserNotFound"]);

        var cert = user.Certifications?.FirstOrDefault(c => c.Id == request.CertificationId)
            ?? throw new KeyNotFoundException(_localizer["CertificationNotFound"]);

        if (!string.IsNullOrEmpty(cert.DocumentUrl))
            await _fileStorageService.DeleteFileAsync(cert.DocumentUrl);

        user.Certifications!.Remove(cert);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
    }
}
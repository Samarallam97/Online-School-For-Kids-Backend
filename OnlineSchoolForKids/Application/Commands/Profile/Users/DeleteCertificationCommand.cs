using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Commands.Profile.Users;
public record DeleteCertificationCommand(string UserId, string CertificationId) : IRequest;

public class DeleteCertificationCommandHandler : IRequestHandler<DeleteCertificationCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteCertificationCommandHandler(IUserRepository userRepository, IFileStorageService fileStorageService)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task Handle(DeleteCertificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var cert = user.Certifications?.FirstOrDefault(c => c.Id == request.CertificationId)
            ?? throw new KeyNotFoundException("Certification not found.");

        if (!string.IsNullOrEmpty(cert.DocumentUrl))
            await _fileStorageService.DeleteFileAsync(cert.DocumentUrl);

        user.Certifications!.Remove(cert);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);
    }
}
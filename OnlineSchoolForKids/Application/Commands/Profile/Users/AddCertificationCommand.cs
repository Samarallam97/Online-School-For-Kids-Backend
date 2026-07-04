using Domain.Entities.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Users;

public class AddCertificationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}

public record AddCertificationCommand(
    string UserId,
    string Name,
    string Issuer,
    string Year,
    IFormFile? File
) : IRequest<CertificationDto>;

public class AddCertificationCommandHandler : IRequestHandler<AddCertificationCommand, CertificationDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AddCertificationCommandHandler(IUserRepository userRepository, IFileStorageService fileStorageService, IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _fileStorageService = fileStorageService;
        _localizer = localizer;
    }

    public async Task<CertificationDto> Handle(AddCertificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException(_localizer["UserNotFound"]);

        if (!int.TryParse(request.Year, out var year))
            throw new ArgumentException(_localizer["InvalidYearFormat"]);

        string? fileUrl = null;
        string? fileName = null;

        if (request.File is not null)
        {
            fileName = request.File.FileName;
            using var stream = request.File.OpenReadStream();
            fileUrl = await _fileStorageService.UploadFileAsync(
                stream,
                fileName,
                $"certifications/{request.UserId}"
            );
        }

        var certification = new Certification
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Issuer = request.Issuer,
            Year = year,
            DocumentUrl = fileUrl
        };

        user.Certifications ??= new List<Certification>();
        user.Certifications.Add(certification);

        await _userRepository.UpdateAsync(user.Id, user, cancellationToken);

        return new CertificationDto
        {
            Id = certification.Id,
            Name = certification.Name,
            Issuer = certification.Issuer,
            Year = certification.Year.ToString(),
            FileUrl = fileUrl,
            FileName = fileName
        };
    }
}
public class CertificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
}

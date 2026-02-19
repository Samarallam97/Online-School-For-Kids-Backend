using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Users;

using Domain.Interfaces.Repositories.Users;
using MediatR;
using Microsoft.Extensions.Configuration;

public record DownloadCertificationQuery(string UserId, string CertificationId) : IRequest<DownloadCertificationResult>;

public class DownloadCertificationQueryHandler : IRequestHandler<DownloadCertificationQuery, DownloadCertificationResult>
{

    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public DownloadCertificationQueryHandler(
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<DownloadCertificationResult> Handle(DownloadCertificationQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var cert = user.Certifications?.FirstOrDefault(c => c.Id == request.CertificationId)
            ?? throw new KeyNotFoundException("Certification not found.");

        if (string.IsNullOrEmpty(cert.DocumentUrl))
            throw new InvalidOperationException("This certification has no attached file.");

        // Resolve physical path from the stored URL (e.g. /uploads/certifications/userId/file.pdf)
        var baseUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";
        var uploadPath = _configuration["FileStorage:UploadPath"] ?? "wwwroot/uploads";

        var relativePath = cert.DocumentUrl.Replace(baseUrl, string.Empty).TrimStart('/');
        var physicalPath = Path.Combine(uploadPath, relativePath);

        if (!System.IO.File.Exists(physicalPath))
            throw new FileNotFoundException("File not found on server.");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath, cancellationToken);
        var fileName = Path.GetFileName(physicalPath);
        var contentType = GetContentType(fileName);

        return new DownloadCertificationResult
        {
            FileData = fileBytes,
            ContentType = contentType,
            FileName = fileName
        };
    }

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
}

public class DownloadCertificationResult
{
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
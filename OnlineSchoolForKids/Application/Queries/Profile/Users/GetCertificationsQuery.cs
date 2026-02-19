using Application.Commands.Profile.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries.Profile.Users;

public record GetCertificationsQuery(string UserId) : IRequest<List<CertificationDto>>;

public class GetCertificationsQueryHandler : IRequestHandler<GetCertificationsQuery, List<CertificationDto>>
{
    private readonly IUserRepository _userRepository;

    public GetCertificationsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<CertificationDto>> Handle(GetCertificationsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return (user.Certifications ?? []).Select(c => new CertificationDto
        {
            Id = c.Id,
            Name = c.Name,
            Issuer = c.Issuer,
            Year = c.Year.ToString(),
            FileUrl = c.DocumentUrl,
            FileName = string.IsNullOrEmpty(c.DocumentUrl)
                ? null
                : Path.GetFileName(c.DocumentUrl)
        }).ToList();
    }
}
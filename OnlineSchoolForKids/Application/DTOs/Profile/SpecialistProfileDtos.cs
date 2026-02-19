using Application.Commands.Profile.Users;
using Application.Queries.Profile.Specialists;
using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Profile;

public class SpecialistProfileDto : BaseProfileDto
{
    public string? ProfessionalTitle { get; set; }
    public List<string> ExpertiseTags { get; set; }
    public List<CertificationDto> Certifications { get; set; }
    public int YearsOfExperience { get; set; }
    public List<AvailabilitySlotDto> Availability { get; set; }
    public decimal HourlyRate { get; set; }
    public SessionRates? SessionRates { get; set; }
    public double Rating { get; set; }
    public int StudentsHelped { get; set; }
}


public class CertificationDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Issuer { get; set; }
    public int Year { get; set; }
    public string? DocumentUrl { get; set; }
}


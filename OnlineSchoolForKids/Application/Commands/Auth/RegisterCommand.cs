using Domain.Entities;
using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Application.Interfaces;
using Application.Models;

namespace Application.Commands.Auth;

public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    UserRole Role
) : IRequest<Result<AuthResponse>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;


    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailService emailService, 
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Validate passwords match
        if (request.Password != request.ConfirmPassword)
        {
            return Result<AuthResponse>.Failure("Passwords do not match.");
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Result<AuthResponse>.Failure("Email is already registered.");
        }

        // Create verification token
        var verificationToken = Guid.NewGuid().ToString();

        // Create user
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLower(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            AuthProvider = AuthProvider.Local,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            EmailVerified = false
        };

        await _userRepository.CreateAsync(user, cancellationToken);

        // Send verification email (fire and forget, don't block registration)
        _ = Task.Run(async () =>
        {
            try
            {
                var verificationLink = $"{_configuration["FrontUrl"]}/verify-email?token={verificationToken}";
                await _emailService.SendVerificationEmailAsync(user.Email, user.FullName,user.EmailVerificationTokenExpiry.Value, verificationLink, cancellationToken);
            }
            catch
            {
                // Log error but don't fail registration
            }
        }, cancellationToken);

     

        return Result<AuthResponse>.Success(new AuthResponse
        {
            User = MapToUserDto(user),
        });
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        EmailVerified = user.EmailVerified,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt = user.CreatedAt
    };
}


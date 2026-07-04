using Application.DTOs;
using Domain.Entities.Users;
using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using Domain.Interfaces.Services.Shared;
using Localization;
using MediatR;
using Microsoft.Extensions.Localization;


namespace Application.Commands.Profile.Admin;

public record CreateAdminRequest(
    string FullName,
    string Email,
    string Password
);

public record CreateAdminCommand(
    string FullName,
    string Email,
    string Password,
    string CallerAdminId
) : IRequest<Result<CreateAdminResponse>>;

public class CreateAdminCommandHandler
    : IRequestHandler<CreateAdminCommand, Result<CreateAdminResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateAdminCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IStringLocalizer<SharedResource> localizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _localizer = localizer;
    }

    public async Task<Result<CreateAdminResponse>> Handle(
        CreateAdminCommand request,
        CancellationToken cancellationToken)
    {
        var caller = await _userRepository.GetByIdAsync(request.CallerAdminId, cancellationToken);
        if (caller is null || caller.Role != UserRole.Admin || caller.IsSuperAdmin != true)
            return Result<CreateAdminResponse>.Failure(
               _localizer["OnlySuperAdminsCanCreateAdminAccounts"]);

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            return Result<CreateAdminResponse>.Failure(
                _localizer["EmailAlreadyExists"]);

        var admin = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLower(),
            EmailVerified = true,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = UserRole.Admin,
            IsSuperAdmin = false,
            Status = UserStatus.Active,
            AuthProvider = AuthProvider.Local,
            IsFirstLogin = true,           // forces password change on first login
            DateOfBirth = DateTime.UtcNow,
            Country = "Unknown",
            CreatedAt = DateTime.UtcNow,
            ActivityLog = new List<ActivityLogEntry>(),
        };

        await _userRepository.CreateAsync(admin, cancellationToken);

        // ── 4. Append to SuperAdmin's activity log ────────────────────────────
        caller.ActivityLog ??= new List<ActivityLogEntry>();
        caller.ActivityLog.Add(new ActivityLogEntry
        {
            Action = "CreateAdmin",
            TargetType = "User",
            Details = $"Created admin account for {request.Email}",
            Timestamp = DateTime.UtcNow,
        });
        await _userRepository.UpdateAsync(caller.Id, caller, cancellationToken);

        return Result<CreateAdminResponse>.Success(new CreateAdminResponse(
            admin.Id,
            admin.FullName,
            admin.Email,
            admin.CreatedAt));
    }
}

public record CreateAdminResponse(
    string Id,
    string FullName,
    string Email,
    DateTime CreatedAt
);





















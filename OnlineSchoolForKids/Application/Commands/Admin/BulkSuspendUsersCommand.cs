using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Admin;

public record BulkSuspendUsersCommand(List<string> UserIds) : IRequest<BulkActionResponse>;

public class BulkSuspendUsersCommandHandler : IRequestHandler<BulkSuspendUsersCommand, BulkActionResponse>
{
    private readonly IUserRepository _userRepo;

    public BulkSuspendUsersCommandHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<BulkActionResponse> Handle(BulkSuspendUsersCommand request, CancellationToken ct)
    {
        var users = await _userRepo.GetManyByIdsAsync(request.UserIds, ct);

        var toUpdate = users.Where(u => u.Status != UserStatus.Suspended).ToList();

        foreach (var user in toUpdate)
        {
            user.Status = UserStatus.Suspended;
            await _userRepo.UpdateAsync(user.Id, user, ct);
        }

        return new BulkActionResponse
        {
            Affected = toUpdate.Count,
            Message  = $"{toUpdate.Count} user(s) suspended successfully.",
        };
    }
}

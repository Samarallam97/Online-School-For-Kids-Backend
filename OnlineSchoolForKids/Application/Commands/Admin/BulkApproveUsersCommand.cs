using Domain.Enums.Users;
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Admin;

public class BulkUserIdsRequest
{
    public List<string> UserIds { get; set; } = new();
}
public record BulkApproveUsersCommand(List<string> UserIds) : IRequest<BulkActionResponse>;

public class BulkApproveUsersCommandHandler : IRequestHandler<BulkApproveUsersCommand, BulkActionResponse>
{
    private readonly IUserRepository _userRepo;

    public BulkApproveUsersCommandHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<BulkActionResponse> Handle(BulkApproveUsersCommand request, CancellationToken ct)
    {
        var users = await _userRepo.GetManyByIdsAsync(request.UserIds, ct);

        // Only touch users that are not already active
        var toUpdate = users.Where(u => u.Status != UserStatus.Active).ToList();

        foreach (var user in toUpdate)
        {
            user.Status = UserStatus.Active;
            await _userRepo.UpdateAsync(user.Id, user, ct);
        }

        return new BulkActionResponse
        {
            Affected = toUpdate.Count,
            Message  = $"{toUpdate.Count} user(s) approved successfully.",
        };
    }
}

public class BulkActionResponse
{
    public int Affected { get; set; }
    public string Message { get; set; } = string.Empty;
}

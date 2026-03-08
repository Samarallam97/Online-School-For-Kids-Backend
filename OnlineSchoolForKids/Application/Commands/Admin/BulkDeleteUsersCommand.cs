using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Admin;

public record BulkDeleteUsersCommand(List<string> UserIds) : IRequest<BulkActionResponse>;

public class BulkDeleteUsersCommandHandler : IRequestHandler<BulkDeleteUsersCommand, BulkActionResponse>
{
    private readonly IUserRepository _userRepo;
    public BulkDeleteUsersCommandHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<BulkActionResponse> Handle(BulkDeleteUsersCommand request, CancellationToken ct)
    {
        var users = await _userRepo.GetManyByIdsAsync(request.UserIds, ct);

        foreach (var user in users)
            await _userRepo.HardDeleteAsync(user.Id, ct);

        return new BulkActionResponse
        {
            Affected = users.Count,
            Message  = $"{users.Count} user(s) permanently deleted.",
        };
    }
}
using Domain.Interfaces.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Admin;

public record DeleteUserCommand(string UserId) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepo;
    public DeleteUserCommandHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var exists = await _userRepo.ExistsAsync(u => u.Id == request.UserId, ct);
        if (!exists)
            throw new KeyNotFoundException($"User '{request.UserId}' not found.");

        // Permanently removes the document from MongoDB
        await _userRepo.HardDeleteAsync(request.UserId, ct);
    }
}
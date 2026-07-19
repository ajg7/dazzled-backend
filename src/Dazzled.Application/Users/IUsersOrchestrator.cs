using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Users;

public interface IUsersOrchestrator
{
    Task<Result<List<User>>> GetUsers(CancellationToken ct = default);
    Task<Result<User>> CreateUser(CreateUserCommand command, CancellationToken ct = default);
    Task<Result<User>> UpdateUser(UpdateUserCommand command, CancellationToken ct = default);
    Task<Result<User>> ChangeEmail(ChangeEmailCommand command, CancellationToken ct = default);
    Task<Result> ChangePassword(ChangePasswordCommand command, CancellationToken ct = default);
}

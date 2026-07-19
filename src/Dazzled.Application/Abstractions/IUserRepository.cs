using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Abstractions;

public interface IUserRepository
{
    /// <summary>Returns <see cref="ResultStatus.NotFound"/> when no user has the given email.</summary>
    Task<Result<User>> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.NotFound"/> when no user has the given id.</summary>
    Task<Result<User>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<List<User>>> GetUsersAsync(CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.Conflict"/> when the email is already taken.</summary>
    Task<Result<User>> CreateUserAsync(User user, CancellationToken ct = default);

    Task<Result<User>> UpdateUserAsync(User user, CancellationToken ct = default);
}

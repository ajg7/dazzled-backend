using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Users;

public class UsersOrchestrator(IUserRepository users, IPasswordHasher passwordHasher) : IUsersOrchestrator
{
    public Task<Result<List<User>>> GetUsers(CancellationToken ct = default) =>
        users.GetUsersAsync(ct);

    public async Task<Result<User>> CreateUser(CreateUserCommand command, CancellationToken ct = default)
    {
        var user = new User
        {
            Name = command.Name,
            Email = command.Email,
            PhoneE164 = command.PhoneE164,
            PasswordHash = passwordHasher.Hash(command.Password),
            Role = command.Role,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return await users.CreateUserAsync(user, ct);
    }

    public async Task<Result<User>> UpdateUser(UpdateUserCommand command, CancellationToken ct = default)
    {
        var lookup = await users.GetByIdAsync(command.Id, ct);
        if (!lookup.IsSuccess)
            return lookup;

        var user = lookup.Value;
        user.Name = command.Name;
        user.PhoneE164 = command.PhoneE164;
        user.Role = command.Role;

        return await users.UpdateUserAsync(user, ct);
    }

    public async Task<Result<User>> ChangeEmail(ChangeEmailCommand command, CancellationToken ct = default)
    {
        var lookup = await users.GetByIdAsync(command.Id, ct);
        if (!lookup.IsSuccess)
            return lookup;

        var user = lookup.Value;

        var verification = VerifyCurrentPassword(user, command.CurrentPassword, command.RequireCurrentPassword);
        if (!verification.IsSuccess)
            return Demote<User>(verification);

        user.Email = command.NewEmail;
        return await users.UpdateUserAsync(user, ct);
    }

    public async Task<Result> ChangePassword(ChangePasswordCommand command, CancellationToken ct = default)
    {
        var lookup = await users.GetByIdAsync(command.Id, ct);
        if (!lookup.IsSuccess)
            return Erase(lookup);

        var user = lookup.Value;

        var verification = VerifyCurrentPassword(user, command.CurrentPassword, command.RequireCurrentPassword);
        if (!verification.IsSuccess)
            return verification;

        user.PasswordHash = passwordHasher.Hash(command.NewPassword);

        var update = await users.UpdateUserAsync(user, ct);
        return update.IsSuccess ? Result.Success() : Erase(update);
    }

    /// <summary>Carries a failed non-generic <see cref="Result"/> over to <see cref="Result{T}"/>.</summary>
    private static Result<T> Demote<T>(Result failed) => failed.Status switch
    {
        ResultStatus.NotFound => Result<T>.NotFound([.. failed.Errors]),
        ResultStatus.Unauthorized => Result<T>.Unauthorized(),
        ResultStatus.Forbidden => Result<T>.Forbidden(),
        ResultStatus.Conflict => Result<T>.Conflict([.. failed.Errors]),
        ResultStatus.Invalid => Result<T>.Invalid([.. failed.ValidationErrors]),
        _ => Result<T>.Error(new ErrorList([.. failed.Errors], null))
    };

    /// <summary>Drops the value from a failed <see cref="Result{T}"/>, preserving its status.</summary>
    private static Result Erase<T>(Result<T> failed) => failed.Status switch
    {
        ResultStatus.NotFound => Result.NotFound([.. failed.Errors]),
        ResultStatus.Unauthorized => Result.Unauthorized(),
        ResultStatus.Forbidden => Result.Forbidden(),
        ResultStatus.Conflict => Result.Conflict([.. failed.Errors]),
        ResultStatus.Invalid => Result.Invalid([.. failed.ValidationErrors]),
        _ => Result.Error(new ErrorList([.. failed.Errors], null))
    };

    private Result VerifyCurrentPassword(User user, string? currentPassword, bool required)
    {
        if (!required)
            return Result.Success();

        if (string.IsNullOrEmpty(currentPassword))
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(currentPassword),
                ErrorMessage = "Current password is required."
            });

        return passwordHasher.Verify(currentPassword, user.PasswordHash)
            ? Result.Success()
            : Result.Unauthorized();
    }
}

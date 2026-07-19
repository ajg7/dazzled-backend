using Ardalis.Result;
using Dazzled.Application.Abstractions;

namespace Dazzled.Application.Auth;

public class AuthOrchestrator(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthOrchestrator
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var lookup = await users.GetByEmailAsync(request.Email, ct);

        // A genuine failure is worth surfacing; an unknown email is not, so that
        // the response cannot be used to probe which accounts exist.
        if (lookup.Status == ResultStatus.Error)
            return Result<LoginResponse>.Error(new ErrorList([.. lookup.Errors], null));

        if (!lookup.IsSuccess || !passwordHasher.Verify(request.Password, lookup.Value.PasswordHash))
            return Result.Unauthorized();

        var user = lookup.Value;
        var token = tokenService.GenerateToken(user);
        return Result.Success(new LoginResponse(token, new UserDto(user.Id, user.Name, user.Email, user.Role)));
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var lookup = await users.GetByIdAsync(userId, ct);
        if (!lookup.IsSuccess)
            return lookup.Map(user => new UserDto(user.Id, user.Name, user.Email, user.Role));

        var user = lookup.Value;
        return Result.Success(new UserDto(user.Id, user.Name, user.Email, user.Role));
    }
}

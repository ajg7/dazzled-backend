using Ardalis.Result;

namespace Dazzled.Application.Auth;

public interface IAuthOrchestrator
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

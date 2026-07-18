using Dazzled.Domain.Enums;

namespace Dazzled.Api.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, UserDto User);

public record UserDto(Guid Id, string Name, string Email, UserRole Role);

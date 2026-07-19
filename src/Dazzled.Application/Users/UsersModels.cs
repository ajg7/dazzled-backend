using Dazzled.Domain.Enums;

namespace Dazzled.Application.Users;

public record CreateUserCommand(string Name, string Email, string Password, string? PhoneE164, UserRole Role);

public record UpdateUserCommand(Guid Id, string Name, string? PhoneE164, UserRole Role);

/// <param name="RequireCurrentPassword">
/// Set by the API layer: true when the caller is changing their own credentials,
/// false for an administrator resetting someone else's.
/// </param>
public record ChangeEmailCommand(Guid Id, string NewEmail, string? CurrentPassword, bool RequireCurrentPassword);

/// <inheritdoc cref="ChangeEmailCommand" path="/param[@name='RequireCurrentPassword']"/>
public record ChangePasswordCommand(Guid Id, string NewPassword, string? CurrentPassword, bool RequireCurrentPassword);

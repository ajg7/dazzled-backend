using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dazzled.Api.Controllers.Users;

public record UserResponse(Guid Id, string Name, string Email, string? PhoneE164, UserRole Role, DateTimeOffset CreatedAtUtc);

public record UserCreationRequest(
    [Required][MaxLength(200)] string Name,
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)] string Password,
    [Phone][MaxLength(20)] string? PhoneE164,
    [Required] UserRole Role);

public record UserUpdateRequest(
    [Required][MaxLength(200)] string Name,
    [Phone][MaxLength(20)] string? PhoneE164,
    [Required] UserRole Role);

/// <param name="CurrentPassword">Required when changing your own email; ignored for an admin acting on another user.</param>
public record ChangeEmailRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    string? CurrentPassword);

/// <inheritdoc cref="ChangeEmailRequest" path="/param[@name='CurrentPassword']"/>
public record ChangePasswordRequest(
    [Required][MinLength(8)] string NewPassword,
    string? CurrentPassword);

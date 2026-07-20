using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.Users;
using Dazzled.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dazzled.Api.Controllers.Users;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public class UsersController(IUsersOrchestrator usersOrchestrator) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<List<UserResponse>>> GetUsers()
    {
        var result = await usersOrchestrator.GetUsers(HttpContext.RequestAborted);
        return result.Map(users => users.Select(ToResponse).ToList());
    }

    [HttpPost]
    [AllowAnonymous]
    [TranslateResultToActionResult]
    public async Task<Result<UserResponse>> CreateUser(UserCreationRequest request)
    {
        var command = new CreateUserCommand(
            request.Name,
            request.Email,
            request.Password,
            request.PhoneE164,
            request.Role);

        var result = await usersOrchestrator.CreateUser(command, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<UserResponse>> UpdateUser(Guid id, UserUpdateRequest request)
    {
        var command = new UpdateUserCommand(
            id,
            request.Name,
            request.PhoneE164,
            request.Role);

        var result = await usersOrchestrator.UpdateUser(command, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPut("{id:guid}/email")]
    [TranslateResultToActionResult]
    public async Task<Result<UserResponse>> ChangeEmail(Guid id, ChangeEmailRequest request)
    {
        if (!TryAuthorizeCredentialChange(id, out var requireCurrentPassword))
            return Result<UserResponse>.Forbidden();

        var command = new ChangeEmailCommand(id, request.Email, request.CurrentPassword, requireCurrentPassword);

        var result = await usersOrchestrator.ChangeEmail(command, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPut("{id:guid}/password")]
    [TranslateResultToActionResult]
    public async Task<Result> ChangePassword(Guid id, ChangePasswordRequest request)
    {
        if (!TryAuthorizeCredentialChange(id, out var requireCurrentPassword))
            return Result.Forbidden();

        var command = new ChangePasswordCommand(id, request.NewPassword, request.CurrentPassword, requireCurrentPassword);

        return await usersOrchestrator.ChangePassword(command, HttpContext.RequestAborted);
    }

    /// <summary>
    /// Credential changes are allowed for the account owner or an administrator.
    /// Owners must prove the current password; an admin resetting someone else's need not.
    /// </summary>
    private bool TryAuthorizeCredentialChange(Guid targetUserId, out bool requireCurrentPassword)
    {
        requireCurrentPassword = true;

        var isSelf = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId)
            && callerId == targetUserId;

        if (isSelf)
            return true;

        if (!User.IsInRole(nameof(UserRole.Admin)))
            return false;

        requireCurrentPassword = false;
        return true;
    }

    private static UserResponse ToResponse(Domain.Entities.User user) =>
        new(user.Id, user.Name, user.Email, user.PhoneE164, user.Role, user.CreatedAtUtc);
}

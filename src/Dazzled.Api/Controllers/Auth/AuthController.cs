using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dazzled.Api.Controllers.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthOrchestrator authOrchestrator) : ControllerBase
{
    [HttpPost("login")]
    [TranslateResultToActionResult]
    public Task<Result<LoginResponse>> Login(LoginRequest request) =>
        authOrchestrator.LoginAsync(request, HttpContext.RequestAborted);

    [Authorize]
    [HttpGet("me")]
    [TranslateResultToActionResult]
    public async Task<Result<UserDto>> Me()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Result.Unauthorized();

        return await authOrchestrator.GetCurrentUserAsync(userId, HttpContext.RequestAborted);
    }
}

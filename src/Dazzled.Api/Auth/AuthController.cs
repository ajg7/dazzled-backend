using Dazzled.Application.Abstractions;
using Dazzled.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Api.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(DazzledDbContext db, ITokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        var token = tokenService.GenerateToken(user);
        return Ok(new LoginResponse(token, new UserDto(user.Id, user.Name, user.Email, user.Role)));
    }
}

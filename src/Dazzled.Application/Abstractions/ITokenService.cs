using Dazzled.Domain;
using System.Security.Claims;

namespace Dazzled.Application.Abstractions;

public interface ITokenService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
}

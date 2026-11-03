using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Revivo.Application.Authentication;

namespace Revivo.Api.Authentication;

public sealed class JwtTokenService(string issuer, string audience, string signingKey, int expirationMinutes)
{
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(signingKey));

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true, ValidIssuer = issuer,
        ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = _key,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        RequireExpirationTime = true, ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    };

    public string CreateToken(AuthenticatedUser user)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };
        var token = new JwtSecurityToken(issuer, audience, claims, now,
            now.AddMinutes(expirationMinutes), new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

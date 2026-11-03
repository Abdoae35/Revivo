using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Authentication;

namespace Revivo.Api.Authentication;

public static class CurrentUserEndpoints
{
    public static IEndpointRouteBuilder MapCurrentUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/me", GetCurrentUserAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> GetCurrentUserAsync(ClaimsPrincipal principal, ILoginService users)
    {
        if (!Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Results.Unauthorized();

        var user = await users.GetUserAsync(userId);
        return user is null ? Results.Unauthorized() : Results.Ok(new { user.Id, user.Name, user.Email });
    }
}

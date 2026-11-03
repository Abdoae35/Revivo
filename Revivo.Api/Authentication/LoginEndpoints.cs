using System.ComponentModel.DataAnnotations;
using Revivo.Application.Authentication;

namespace Revivo.Api.Authentication;

public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", LoginAsync);
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, ILoginService login, JwtTokenService tokens)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 256 ||
            !new EmailAddressAttribute().IsValid(request.Email.Trim()) || string.IsNullOrEmpty(request.Password))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Credentials"] = ["A valid email and password are required."] });

        var user = await login.ValidateCredentialsAsync(request.Email.Trim(), request.Password);
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(new { accessToken = tokens.CreateToken(user) });
    }
}

public sealed record LoginRequest(string? Email, string? Password);

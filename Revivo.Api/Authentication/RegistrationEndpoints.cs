using System.ComponentModel.DataAnnotations;
using Revivo.Application.Authentication;

namespace Revivo.Api.Authentication;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", RegisterAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, IRegistrationService registrations)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            errors[nameof(request.Name)] = ["Name is required and must not exceed 100 characters."];
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 256 ||
            !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            errors[nameof(request.Email)] = ["A valid email address of at most 256 characters is required."];
        if (string.IsNullOrEmpty(request.Password))
            errors[nameof(request.Password)] = ["Password is required."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var result = await registrations.RegisterAsync(request.Name!.Trim(), request.Email!.Trim(), request.Password!);
        if (result.DuplicateEmail)
            return Results.Conflict(new { message = "An account with this email already exists." });
        if (result.Errors.Count > 0)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Password"] = result.Errors.ToArray() });

        return Results.Json(new { id = result.UserId }, statusCode: StatusCodes.Status201Created);
    }
}

public sealed record RegisterRequest(string? Name, string? Email, string? Password);

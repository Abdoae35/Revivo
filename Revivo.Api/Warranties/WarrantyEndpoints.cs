using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Warranties;

namespace Revivo.Api.Warranties;

public static class WarrantyEndpoints
{
    public static IEndpointRouteBuilder MapWarrantyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles/{vehicleId:int}").RequireAuthorization();
        group.MapPut("/maintenance/{recordId:int}/warranty", UpsertAsync);
        group.MapGet("/warranties", GetAllAsync);
        group.MapGet("/maintenance/{recordId:int}/warranty", GetAsync);
        group.MapDelete("/maintenance/{recordId:int}/warranty", DeleteAsync);
        return endpoints;
    }

    private static async Task<IResult> UpsertAsync(int vehicleId, int recordId, WarrantyInput input,
        ClaimsPrincipal principal, IWarrantyService warranties, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var errors = Validate(vehicleId, recordId, input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var result = await warranties.UpsertAsync(ownerId, vehicleId, recordId, input, cancellationToken);
        return result.Status switch
        {
            WarrantyWriteStatus.RecordNotFound => Results.NotFound(),
            WarrantyWriteStatus.Created => Results.Created(
                $"/api/vehicles/{vehicleId}/maintenance/{recordId}/warranty", result.Warranty),
            _ => Results.Ok(result.Warranty)
        };
    }

    private static async Task<IResult> GetAllAsync(int vehicleId, ClaimsPrincipal principal,
        IWarrantyService warranties, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0) return InvalidVehicleId();
        var items = await warranties.GetAllAsync(ownerId, vehicleId, cancellationToken);
        return items is null ? Results.NotFound() : Results.Ok(items);
    }

    private static async Task<IResult> GetAsync(int vehicleId, int recordId, ClaimsPrincipal principal,
        IWarrantyService warranties, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0 || recordId <= 0) return InvalidIds();
        var warranty = await warranties.GetAsync(ownerId, vehicleId, recordId, cancellationToken);
        return warranty is null ? Results.NotFound() : Results.Ok(warranty);
    }

    private static async Task<IResult> DeleteAsync(int vehicleId, int recordId, ClaimsPrincipal principal,
        IWarrantyService warranties, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0 || recordId <= 0) return InvalidIds();
        return await warranties.DeleteAsync(ownerId, vehicleId, recordId, cancellationToken)
            ? Results.NoContent() : Results.NotFound();
    }

    private static Dictionary<string, string[]> Validate(int vehicleId, int recordId, WarrantyInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (vehicleId <= 0) errors["vehicleId"] = ["A positive vehicle ID is required."];
        if (recordId <= 0) errors["recordId"] = ["A positive maintenance record ID is required."];
        if (input.DurationMonths is < 1 or > 1200)
            errors["durationMonths"] = ["Warranty duration must be between 1 and 1200 months."];
        if (input.StartDate is DateOnly start &&
            (start < new DateOnly(1886, 1, 1) || start > DateOnly.FromDateTime(DateTime.UtcNow)))
            errors["startDate"] = ["Warranty start date must be valid and cannot be in the future."];
        if (input.WarningDays is < 0) errors["warningDays"] = ["Warning days cannot be negative."];
        if (input.Provider?.Length > 150) errors["provider"] = ["Provider cannot exceed 150 characters."];
        if (input.Brand?.Length > 150) errors["brand"] = ["Brand cannot exceed 150 characters."];
        if (input.Notes?.Length > 2000) errors["notes"] = ["Notes cannot exceed 2000 characters."];
        return errors;
    }

    private static IResult InvalidVehicleId() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["vehicleId"] = ["A positive vehicle ID is required."] });
    private static IResult InvalidIds() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["recordId"] = ["A positive vehicle and maintenance record ID are required."] });

    private static bool TryOwner(ClaimsPrincipal principal, out Guid ownerId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out ownerId) && ownerId != Guid.Empty;
}

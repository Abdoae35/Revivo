using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Vehicles;

namespace Revivo.Api.Vehicles;

public static class OdometerEndpoints
{
    public static IEndpointRouteBuilder MapOdometerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles/{vehicleId:int}/odometer").RequireAuthorization();
        group.MapPost("", RecordAsync);
        group.MapGet("/history", GetHistoryAsync);
        return endpoints;
    }

    private static async Task<IResult> RecordAsync(int vehicleId, OdometerInput input, ClaimsPrincipal principal,
        IOdometerService odometer, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        if (input.MileageKm < 0) return Invalid("mileageKm", "Mileage cannot be negative.");
        var result = await odometer.RecordAsync(ownerId, vehicleId, input.MileageKm, cancellationToken);
        return result.Status switch
        {
            OdometerWriteStatus.NotFound => Results.NotFound(),
            OdometerWriteStatus.LowerMileage => Invalid("mileageKm", "Mileage cannot be lower than the vehicle's current mileage."),
            _ => Results.Ok(result.Update)
        };
    }

    private static async Task<IResult> GetHistoryAsync(int vehicleId, int? page, int? pageSize,
        ClaimsPrincipal principal, IOdometerService odometer, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        if (page is <= 0) return Invalid("page", "Page must be positive.");
        if (pageSize is <= 0 or > 100) return Invalid("pageSize", "Page size must be between 1 and 100.");
        var history = await odometer.GetHistoryAsync(ownerId, vehicleId, page ?? 1, pageSize ?? 20, cancellationToken);
        return history is null ? Results.NotFound() : Results.Ok(history);
    }

    private static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static bool TryOwner(ClaimsPrincipal principal, out Guid ownerId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out ownerId) && ownerId != Guid.Empty;
}

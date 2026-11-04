using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Maintenance;

namespace Revivo.Api.Maintenance;

public static class PeriodicMaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapPeriodicMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles/{vehicleId:int}").RequireAuthorization();
        group.MapPut("/maintenance-items/{itemId:int}/schedule", ConfigureAsync);
        group.MapGet("/maintenance-status", GetStatusesAsync);
        group.MapGet("/upcoming-maintenance", GetUpcomingAsync);
        return endpoints;
    }

    private static async Task<IResult> ConfigureAsync(int vehicleId, int itemId, ScheduleInput input,
        ClaimsPrincipal principal, IPeriodicMaintenanceService periodic, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var errors = new Dictionary<string, string[]>();
        if (vehicleId <= 0) errors["vehicleId"] = ["A positive vehicle ID is required."];
        if (itemId <= 0) errors["itemId"] = ["A positive maintenance item ID is required."];
        if (input.IntervalKm is <= 0) errors["intervalKm"] = ["Mileage interval must be positive."];
        if (input.IntervalMonths is <= 0 or > 1200)
            errors["intervalMonths"] = ["Time interval must be between 1 and 1200 months."];
        if (input.WarningKm is < 0) errors["warningKm"] = ["Mileage warning threshold cannot be negative."];
        if (input.WarningDays is < 0) errors["warningDays"] = ["Day warning threshold cannot be negative."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        var result = await periodic.ConfigureAsync(ownerId, vehicleId, itemId, input, cancellationToken);
        return result.Status == ScheduleWriteStatus.Success ? Results.Ok(result.Item) : Results.NotFound();
    }

    private static async Task<IResult> GetStatusesAsync(int vehicleId, ClaimsPrincipal principal,
        IPeriodicMaintenanceService periodic, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0) return InvalidVehicleId();
        var statuses = await periodic.GetStatusesAsync(ownerId, vehicleId, cancellationToken);
        return statuses is null ? Results.NotFound() : Results.Ok(statuses);
    }

    private static async Task<IResult> GetUpcomingAsync(int vehicleId, ClaimsPrincipal principal,
        IPeriodicMaintenanceService periodic, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0) return InvalidVehicleId();
        var items = await periodic.GetUpcomingAsync(ownerId, vehicleId, cancellationToken);
        return items is null ? Results.NotFound() : Results.Ok(items);
    }

    private static IResult InvalidVehicleId() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["vehicleId"] = ["A positive vehicle ID is required."] });

    private static bool TryOwner(ClaimsPrincipal principal, out Guid ownerId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out ownerId) && ownerId != Guid.Empty;
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Maintenance;

namespace Revivo.Api.Maintenance;

public static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles/{vehicleId:int}/maintenance").RequireAuthorization();
        group.MapPost("", CreateAsync);
        group.MapGet("", GetPageAsync);
        group.MapGet("/{id:int}", GetAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(int vehicleId, MaintenanceInput input, ClaimsPrincipal principal,
        IMaintenanceService maintenance, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var errors = Validate(vehicleId, input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var result = await maintenance.CreateAsync(ownerId, vehicleId, input, cancellationToken);
        return result.Status switch
        {
            MaintenanceWriteStatus.VehicleNotFound => Results.NotFound(),
            MaintenanceWriteStatus.InvalidDate => InvalidVehicleDate(),
            _ => Results.Created($"/api/vehicles/{vehicleId}/maintenance/{result.Record!.Id}", result.Record)
        };
    }

    private static async Task<IResult> GetPageAsync(int vehicleId, int? page, int? pageSize,
        ClaimsPrincipal principal, IMaintenanceService maintenance, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var actualPage = page ?? 1;
        var actualSize = pageSize ?? 20;
        var errors = new Dictionary<string, string[]>();
        if (vehicleId <= 0) errors["vehicleId"] = ["A positive vehicle ID is required."];
        if (actualPage < 1 || actualPage > int.MaxValue / Math.Max(actualSize, 1))
            errors["page"] = ["Page must be positive and within the supported range."];
        if (actualSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var result = await maintenance.GetPageAsync(ownerId, vehicleId, actualPage, actualSize, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetAsync(int vehicleId, int id, ClaimsPrincipal principal,
        IMaintenanceService maintenance, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0 || id <= 0) return Results.BadRequest();
        var record = await maintenance.GetAsync(ownerId, vehicleId, id, cancellationToken);
        return record is null ? Results.NotFound() : Results.Ok(record);
    }

    private static async Task<IResult> UpdateAsync(int vehicleId, int id, MaintenanceInput input,
        ClaimsPrincipal principal, IMaintenanceService maintenance, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var errors = Validate(vehicleId, input);
        if (id <= 0) errors["id"] = ["A positive maintenance record ID is required."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var result = await maintenance.UpdateAsync(ownerId, vehicleId, id, input, cancellationToken);
        return result.Status switch
        {
            MaintenanceWriteStatus.VehicleNotFound or MaintenanceWriteStatus.RecordNotFound => Results.NotFound(),
            MaintenanceWriteStatus.InvalidDate => InvalidVehicleDate(),
            _ => Results.Ok(result.Record)
        };
    }

    private static async Task<IResult> DeleteAsync(int vehicleId, int id, ClaimsPrincipal principal,
        IMaintenanceService maintenance, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        if (vehicleId <= 0 || id <= 0) return Results.BadRequest();
        var result = await maintenance.DeleteAsync(ownerId, vehicleId, id, cancellationToken);
        return result switch
        {
            MaintenanceWriteStatus.Success => Results.NoContent(),
            MaintenanceWriteStatus.HasWarranty => Results.Conflict(new { message = "Delete the warranty before deleting this maintenance record." }),
            _ => Results.NotFound()
        };
    }

    private static Dictionary<string, string[]> Validate(int vehicleId, MaintenanceInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (vehicleId <= 0) errors["vehicleId"] = ["A positive vehicle ID is required."];
        if (string.IsNullOrWhiteSpace(input.Category) || input.Category.Trim().Length > 100)
            errors["category"] = ["Category is required and must not exceed 100 characters."];
        if (string.IsNullOrWhiteSpace(input.ItemName) || input.ItemName.Trim().Length > 100)
            errors["itemName"] = ["Item name is required and must not exceed 100 characters."];
        if (input.MaintenanceDate < new DateOnly(1886, 1, 1) ||
            input.MaintenanceDate > DateOnly.FromDateTime(DateTime.UtcNow))
            errors["maintenanceDate"] = ["Maintenance date must be valid and cannot be in the future."];
        if (input.MileageKm < 0) errors["mileageKm"] = ["Mileage cannot be negative."];
        if (input.CostEgp < 0 || input.CostEgp > 9999999999999999.99m ||
            decimal.Round(input.CostEgp, 2) != input.CostEgp)
            errors["costEgp"] = ["Cost must be nonnegative EGP with at most two decimal places."];
        if (input.Notes?.Length > 2000) errors["notes"] = ["Notes cannot exceed 2000 characters."];
        return errors;
    }

    private static IResult InvalidVehicleDate() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["maintenanceDate"] = ["Date cannot precede the vehicle year."] });

    private static bool TryOwner(ClaimsPrincipal principal, out Guid ownerId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out ownerId) && ownerId != Guid.Empty;
}

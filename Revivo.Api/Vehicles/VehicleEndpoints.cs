using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Vehicles;

namespace Revivo.Api.Vehicles;

public static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles").RequireAuthorization();
        group.MapPost("", CreateAsync);
        group.MapGet("", GetAllAsync);
        group.MapGet("/{id:int}", GetAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(VehicleInput input, ClaimsPrincipal principal,
        IVehicleService vehicles, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var errors = Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var result = await vehicles.CreateAsync(ownerId, input, cancellationToken);
        if (result.Status == VehicleWriteStatus.InvalidModel)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["CarModelId"] = ["Car model does not exist."] });
        return Results.Created($"/api/vehicles/{result.Vehicle!.Id}", result.Vehicle);
    }

    private static async Task<IResult> GetAllAsync(ClaimsPrincipal principal, IVehicleService vehicles, CancellationToken cancellationToken) =>
        TryOwner(principal, out var ownerId) ? Results.Ok(await vehicles.GetAllAsync(ownerId, cancellationToken)) : Results.Unauthorized();

    private static async Task<IResult> GetAsync(int id, ClaimsPrincipal principal, IVehicleService vehicles, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var vehicle = await vehicles.GetAsync(ownerId, id, cancellationToken);
        return vehicle is null ? Results.NotFound() : Results.Ok(vehicle);
    }

    private static async Task<IResult> UpdateAsync(int id, VehicleInput input, ClaimsPrincipal principal,
        IVehicleService vehicles, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        var errors = Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var result = await vehicles.UpdateAsync(ownerId, id, input, cancellationToken);
        return result.Status switch
        {
            VehicleWriteStatus.NotFound => Results.NotFound(),
            VehicleWriteStatus.InvalidModel => Results.ValidationProblem(new Dictionary<string, string[]> { ["CarModelId"] = ["Car model does not exist."] }),
            VehicleWriteStatus.MileageBelowMaintenance => Results.ValidationProblem(new Dictionary<string, string[]> { ["CurrentMileageKm"] = ["Mileage cannot be below recorded maintenance mileage."] }),
            _ => Results.Ok(result.Vehicle)
        };
    }

    private static async Task<IResult> DeleteAsync(int id, ClaimsPrincipal principal, IVehicleService vehicles, CancellationToken cancellationToken)
    {
        if (!TryOwner(principal, out var ownerId)) return Results.Unauthorized();
        return await vehicles.DeleteAsync(ownerId, id, cancellationToken) switch
        {
            VehicleDeleteStatus.Deleted => Results.NoContent(),
            VehicleDeleteStatus.HasMaintenance => Results.Conflict(new { message = "Delete maintenance records before deleting this vehicle." }),
            _ => Results.NotFound()
        };
    }

    private static bool TryOwner(ClaimsPrincipal principal, out Guid ownerId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out ownerId) && ownerId != Guid.Empty;

    private static Dictionary<string, string[]> Validate(VehicleInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (input.CarModelId <= 0) errors["CarModelId"] = ["A car model is required."];
        if (input.Year < 1886 || input.Year > DateTime.UtcNow.Year + 1)
            errors["Year"] = ["Vehicle year is outside the valid range."];
        if (input.CurrentMileageKm < 0) errors["CurrentMileageKm"] = ["Mileage cannot be negative."];
        if (input.Nickname?.Length > 100) errors["Nickname"] = ["Nickname cannot exceed 100 characters."];
        if (input.ImageUrl?.Length > 2048) errors["ImageUrl"] = ["Image URL cannot exceed 2048 characters."];
        return errors;
    }
}

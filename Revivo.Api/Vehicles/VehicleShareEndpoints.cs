using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Vehicles;

namespace Revivo.Api.Vehicles;

public static class VehicleShareEndpoints
{
    public static IEndpointRouteBuilder MapVehicleShareEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var vehicles = endpoints.MapGroup("/api/vehicles").RequireAuthorization();
        vehicles.MapGet("/shared", GetSharedVehiclesAsync);
        vehicles.MapPost("/{vehicleId:int}/shares", InviteAsync);
        vehicles.MapGet("/{vehicleId:int}/shares", GetVehicleSharesAsync);
        vehicles.MapDelete("/{vehicleId:int}/shares/{shareId:int}", RevokeAsync);

        var invitations = endpoints.MapGroup("/api/vehicle-invitations").RequireAuthorization();
        invitations.MapGet("", GetInvitationsAsync);
        invitations.MapPost("/{shareId:int}/accept", AcceptAsync);
        invitations.MapPost("/{shareId:int}/reject", RejectAsync);
        return endpoints;
    }

    private static async Task<IResult> InviteAsync(int vehicleId, VehicleInviteInput input,
        ClaimsPrincipal principal, IVehicleShareService shares, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Trim().Length > 256 ||
            !new EmailAddressAttribute().IsValid(input.Email.Trim()))
            return Invalid("email", "A valid registered email address is required.");
        var result = await shares.InviteAsync(userId, vehicleId, input.Email.Trim(), cancellationToken);
        return result.Status switch
        {
            InviteStatus.VehicleNotFound => Results.NotFound(),
            InviteStatus.UserNotFound => Invalid("email", "No registered user has this email address."),
            InviteStatus.SelfInvite => Invalid("email", "You cannot invite yourself."),
            InviteStatus.Duplicate => Results.Conflict(new { message = "An active invitation or share already exists." }),
            _ => Results.Json(result.Share, statusCode: StatusCodes.Status201Created)
        };
    }

    private static async Task<IResult> GetInvitationsAsync(ClaimsPrincipal principal,
        IVehicleShareService shares, CancellationToken cancellationToken) =>
        TryUser(principal, out var userId)
            ? Results.Ok(await shares.GetInvitationsAsync(userId, cancellationToken))
            : Results.Unauthorized();

    private static async Task<IResult> AcceptAsync(int shareId, ClaimsPrincipal principal,
        IVehicleShareService shares, CancellationToken cancellationToken) =>
        await RespondAsync(shareId, true, principal, shares, cancellationToken);

    private static async Task<IResult> RejectAsync(int shareId, ClaimsPrincipal principal,
        IVehicleShareService shares, CancellationToken cancellationToken) =>
        await RespondAsync(shareId, false, principal, shares, cancellationToken);

    private static async Task<IResult> RespondAsync(int shareId, bool accept, ClaimsPrincipal principal,
        IVehicleShareService shares, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (shareId <= 0) return Invalid("shareId", "A positive invitation ID is required.");
        var result = await shares.RespondAsync(userId, shareId, accept, cancellationToken);
        return result.Status switch
        {
            InvitationResponseStatus.NotFound => Results.NotFound(),
            InvitationResponseStatus.NotPending => Results.Conflict(new { message = "Invitation is no longer pending." }),
            _ => Results.Ok(result.Share)
        };
    }

    private static async Task<IResult> GetVehicleSharesAsync(int vehicleId, ClaimsPrincipal principal,
        IVehicleShareService shares, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        var result = await shares.GetVehicleSharesAsync(userId, vehicleId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> RevokeAsync(int vehicleId, int shareId, ClaimsPrincipal principal,
        IVehicleShareService shares, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        if (shareId <= 0) return Invalid("shareId", "A positive share ID is required.");
        return await shares.RevokeAsync(userId, vehicleId, shareId, cancellationToken)
            ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetSharedVehiclesAsync(ClaimsPrincipal principal,
        IVehicleShareService shares, CancellationToken cancellationToken) =>
        TryUser(principal, out var userId)
            ? Results.Ok(await shares.GetSharedVehiclesAsync(userId, cancellationToken))
            : Results.Unauthorized();

    private static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static bool TryUser(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId) && userId != Guid.Empty;
}

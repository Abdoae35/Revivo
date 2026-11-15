using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Notifications;
using Revivo.Domain.Entities;

namespace Revivo.Api.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles/{vehicleId:int}").RequireAuthorization();
        group.MapGet("/notification-settings", GetSettingsAsync);
        group.MapPut("/notification-settings", UpdateSettingsAsync);
        group.MapGet("/alerts", GetAlertsAsync);
        return endpoints;
    }

    private static async Task<IResult> GetSettingsAsync(int vehicleId, ClaimsPrincipal principal,
        INotificationService notifications, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return InvalidVehicle();
        var settings = await notifications.GetSettingsAsync(userId, vehicleId, cancellationToken);
        return settings is null ? Results.NotFound() : Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSettingsAsync(int vehicleId, NotificationSettingsInput input,
        ClaimsPrincipal principal, INotificationService notifications, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        var errors = new Dictionary<string, string[]>();
        if (vehicleId <= 0) errors["vehicleId"] = ["A positive vehicle ID is required."];
        if (input.ReminderDayOfWeek is null or < 0 or > 6)
            errors["reminderDayOfWeek"] = ["Day must be between Sunday (0) and Saturday (6)."];
        if (input.ReminderTime is null) errors["reminderTime"] = ["A valid reminder time is required."];
        if (!NotificationSettings.IsValidTimeZone(input.TimeZoneId))
            errors["timeZoneId"] = ["A valid time zone ID is required."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var settings = await notifications.UpdateSettingsAsync(userId, vehicleId, input, cancellationToken);
        return settings is null ? Results.NotFound() : Results.Ok(settings);
    }

    private static async Task<IResult> GetAlertsAsync(int vehicleId, ClaimsPrincipal principal,
        INotificationService notifications, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return InvalidVehicle();
        var alerts = await notifications.GetAlertsAsync(userId, vehicleId, cancellationToken);
        return alerts is null ? Results.NotFound() : Results.Ok(alerts);
    }

    private static IResult InvalidVehicle() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["vehicleId"] = ["A positive vehicle ID is required."] });

    private static bool TryUser(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId) && userId != Guid.Empty;
}

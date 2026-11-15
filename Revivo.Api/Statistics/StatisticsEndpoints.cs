using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Revivo.Application.Statistics;

namespace Revivo.Api.Statistics;

public static class StatisticsEndpoints
{
    public static IEndpointRouteBuilder MapStatisticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles/{vehicleId:int}/statistics").RequireAuthorization();
        group.MapGet("/overview", GetOverviewAsync);
        group.MapGet("/mileage", GetMileageAsync);
        group.MapGet("/expenses", GetExpensesAsync);
        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(int vehicleId, ClaimsPrincipal principal,
        IStatisticsService statistics, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        var overview = await statistics.GetOverviewAsync(userId, vehicleId, cancellationToken);
        return overview is null ? Results.NotFound() : Results.Ok(overview);
    }

    private static async Task<IResult> GetMileageAsync(int vehicleId, int? year, ClaimsPrincipal principal,
        IStatisticsService statistics, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        var selectedYear = year ?? DateTime.UtcNow.Year;
        if (selectedYear is < 1886 || selectedYear > DateTime.UtcNow.Year)
            return Invalid("year", "Year must be between 1886 and the current year.");
        var mileage = await statistics.GetMileageAsync(userId, vehicleId, selectedYear, cancellationToken);
        return mileage is null ? Results.NotFound() : Results.Ok(mileage);
    }

    private static async Task<IResult> GetExpensesAsync(int vehicleId, int? year, ClaimsPrincipal principal,
        IStatisticsService statistics, CancellationToken cancellationToken)
    {
        if (!TryUser(principal, out var userId)) return Results.Unauthorized();
        if (vehicleId <= 0) return Invalid("vehicleId", "A positive vehicle ID is required.");
        var selectedYear = year ?? DateTime.UtcNow.Year;
        if (selectedYear is < 1886 || selectedYear > DateTime.UtcNow.Year)
            return Invalid("year", "Year must be between 1886 and the current year.");
        var expenses = await statistics.GetExpensesAsync(userId, vehicleId, selectedYear, cancellationToken);
        return expenses is null ? Results.NotFound() : Results.Ok(expenses);
    }

    private static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static bool TryUser(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId) && userId != Guid.Empty;
}

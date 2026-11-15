using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Revivo.Api.Statistics;
using Revivo.Application.Statistics;

namespace Revivo.Api.Tests;

public class StatisticsEndpointTests
{
    [Fact]
    public async Task EndpointsRequireJwtOwnerAndRejectMissingVehicle()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingStatistics(owner);
        var overview = Method("GetOverviewAsync");
        var mileage = Method("GetMileageAsync");
        var expenses = Method("GetExpensesAsync");
        Assert.Equal(401, await Status(overview, [7, new ClaimsPrincipal(), service, CancellationToken.None]));
        Assert.Equal(401, await Status(mileage, [7, 2026, new ClaimsPrincipal(), service, CancellationToken.None]));
        Assert.Equal(401, await Status(expenses, [7, 2026, new ClaimsPrincipal(), service, CancellationToken.None]));
        Assert.Equal(404, await Status(overview, [7, Principal(Guid.NewGuid()), service, CancellationToken.None]));
        Assert.Equal(404, await Status(mileage, [7, 2026, Principal(Guid.NewGuid()), service, CancellationToken.None]));
        Assert.Equal(404, await Status(expenses, [7, 2026, Principal(Guid.NewGuid()), service, CancellationToken.None]));
        Assert.Equal(404, await Status(overview, [999, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(200, await Status(overview, [7, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(owner, service.LastUser);
    }

    [Fact]
    public async Task InvalidYearsReturnBadRequestWithoutQueryingService()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingStatistics(owner);
        Assert.Equal(400, await Status(Method("GetMileageAsync"),
            [7, 1800, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(400, await Status(Method("GetExpensesAsync"),
            [7, DateTime.UtcNow.Year + 1, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(0, service.Calls);
        Assert.Equal(200, await Status(Method("GetMileageAsync"),
            [7, null, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(DateTime.UtcNow.Year, service.LastYear);
    }

    private static MethodInfo Method(string name) =>
        typeof(StatisticsEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    private static async Task<int?> Status(MethodInfo method, object?[] args) =>
        ((IStatusCodeHttpResult)await (Task<IResult>)method.Invoke(null, args)!).StatusCode;

    private static ClaimsPrincipal Principal(Guid userId) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())], "test"));

    private sealed class RecordingStatistics(Guid owner) : IStatisticsService
    {
        public Guid LastUser { get; private set; }
        public int LastYear { get; private set; }
        public int Calls { get; private set; }

        public Task<StatisticsOverviewDto?> GetOverviewAsync(Guid userId, int vehicleId,
            CancellationToken cancellationToken)
        {
            LastUser = userId; Calls++;
            return Task.FromResult<StatisticsOverviewDto?>(userId == owner && vehicleId == 7
                ? new(7, 1000, null, "Unavailable", false, null, "Unavailable", false,
                    null, 0, 0, 0, 0, "EGP") : null);
        }

        public Task<MileageStatisticsDto?> GetMileageAsync(Guid userId, int vehicleId, int year,
            CancellationToken cancellationToken)
        {
            LastYear = year; Calls++;
            return Task.FromResult<MileageStatisticsDto?>(userId == owner && vehicleId == 7
                ? new(7, year, 1000, null, "Unavailable", false, null, []) : null);
        }

        public Task<ExpenseStatisticsDto?> GetExpensesAsync(Guid userId, int vehicleId, int year,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<ExpenseStatisticsDto?>(userId == owner && vehicleId == 7
                ? new(7, year, 0, 0, "EGP", [], [], []) : null);
        }
    }
}

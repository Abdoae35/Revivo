using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Revivo.Api.Maintenance;
using Revivo.Application.Maintenance;
using Revivo.Domain.Entities;

namespace Revivo.Api.Tests;

public class MaintenanceEndpointTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task CreateUsesJwtOwnerAndValidatesBeforeWriting()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingMaintenance(owner);
        var method = typeof(MaintenanceEndpoints).GetMethod("CreateAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var input = new MaintenanceInput("Fluids", "Engine Oil", Today, 12000, 850.50m, null);

        var created = await (Task<IResult>)method.Invoke(null, [7, input, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(201, ((IStatusCodeHttpResult)created).StatusCode);
        Assert.Equal(owner, service.LastOwner);
        Assert.Equal(7, service.LastVehicleId);
        Assert.Equal(1, service.CreateCalls);

        var invalid = input with { MileageKm = -1, CostEgp = -1, MaintenanceDate = Today.AddDays(1) };
        var rejected = await (Task<IResult>)method.Invoke(null, [7, invalid, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(400, ((IStatusCodeHttpResult)rejected).StatusCode);
        Assert.Equal(1, service.CreateCalls);
    }

    [Fact]
    public async Task MissingJwtClaimIsUnauthorized()
    {
        var method = typeof(MaintenanceEndpoints).GetMethod("CreateAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var input = new MaintenanceInput("Fluids", "Oil", Today, 1, 1m, null);
        var result = await (Task<IResult>)method.Invoke(null,
            [7, input, new ClaimsPrincipal(), new RecordingMaintenance(Guid.NewGuid()), CancellationToken.None])!;
        Assert.Equal(401, ((IStatusCodeHttpResult)result).StatusCode);
    }

    [Fact]
    public async Task HistoryIsPagedAndOtherOwnerGetsNotFound()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingMaintenance(owner);
        var method = typeof(MaintenanceEndpoints).GetMethod("GetPageAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

        var page = await (Task<IResult>)method.Invoke(null, [7, 2, 5, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(200, ((IStatusCodeHttpResult)page).StatusCode);
        Assert.Equal(2, service.LastPage);
        Assert.Equal(5, service.LastPageSize);

        var forbidden = await (Task<IResult>)method.Invoke(null,
            [7, 1, 20, Principal(Guid.NewGuid()), service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)forbidden).StatusCode);
        var invalid = await (Task<IResult>)method.Invoke(null,
            [7, 0, 101, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(400, ((IStatusCodeHttpResult)invalid).StatusCode);
    }

    [Fact]
    public void DomainRejectsInvalidMileageCostAndDate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaintenanceRecord { MaintenanceDate = Today.AddDays(1) });
        var record = new MaintenanceRecord { MaintenanceDate = Today };
        Assert.Throws<ArgumentOutOfRangeException>(() => record.MileageKm = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => record.CostEgp = -0.01m);
        Assert.Throws<ArgumentOutOfRangeException>(() => record.CostEgp = 1.001m);
    }

    private static ClaimsPrincipal Principal(Guid owner) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, owner.ToString())], "test"));

    private sealed class RecordingMaintenance(Guid owner) : IMaintenanceService
    {
        public Guid LastOwner { get; private set; }
        public int LastVehicleId { get; private set; }
        public int CreateCalls { get; private set; }
        public int LastPage { get; private set; }
        public int LastPageSize { get; private set; }

        public Task<MaintenanceWriteResult> CreateAsync(Guid ownerId, int vehicleId, MaintenanceInput input,
            CancellationToken cancellationToken)
        {
            LastOwner = ownerId;
            LastVehicleId = vehicleId;
            CreateCalls++;
            return Task.FromResult(new MaintenanceWriteResult(MaintenanceWriteStatus.Success,
                new MaintenanceDto(1, vehicleId, 1, input.Category!, input.ItemName!, input.MaintenanceDate,
                    input.MileageKm, input.CostEgp, input.Notes, DateTimeOffset.UtcNow)));
        }

        public Task<MaintenancePage?> GetPageAsync(Guid ownerId, int vehicleId, int page, int pageSize,
            CancellationToken cancellationToken)
        {
            LastPage = page;
            LastPageSize = pageSize;
            return Task.FromResult<MaintenancePage?>(ownerId == owner
                ? new MaintenancePage(page, pageSize, 0, []) : null);
        }

        public Task<MaintenanceDto?> GetAsync(Guid ownerId, int vehicleId, int id, CancellationToken cancellationToken) =>
            Task.FromResult<MaintenanceDto?>(null);
        public Task<MaintenanceWriteResult> UpdateAsync(Guid ownerId, int vehicleId, int id, MaintenanceInput input,
            CancellationToken cancellationToken) => Task.FromResult(new MaintenanceWriteResult(MaintenanceWriteStatus.RecordNotFound));
        public Task<MaintenanceWriteStatus> DeleteAsync(Guid ownerId, int vehicleId, int id,
            CancellationToken cancellationToken) => Task.FromResult(MaintenanceWriteStatus.RecordNotFound);
    }
}

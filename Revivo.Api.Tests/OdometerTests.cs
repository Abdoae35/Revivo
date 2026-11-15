using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Revivo.Api.Vehicles;
using Revivo.Application.Vehicles;
using Revivo.Domain.Entities;
using Revivo.Domain.Maintenance;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Api.Tests;

public class OdometerTests
{
    [Fact]
    public void ReadingRejectsNegativeMileage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OdometerReading { MileageKm = -1 });
    }

    [Fact]
    public void ReadingMappingHasVehicleForeignKeyAndHistoryIndex()
    {
        var options = new DbContextOptionsBuilder<RevivoDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Trusted_Connection=True").Options;
        using var db = new RevivoDbContext(options);
        var entity = db.Model.FindEntityType(typeof(OdometerReading))!;
        Assert.Equal("OdometerReadings", entity.GetTableName());
        Assert.Equal(typeof(Vehicle), Assert.Single(entity.GetForeignKeys()).PrincipalEntityType.ClrType);
        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(OdometerReading.VehicleId), nameof(OdometerReading.RecordedAt), nameof(OdometerReading.Id)]));
    }

    [Fact]
    public async Task RecordValidatesInputOwnerAndDecrease()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingOdometer(owner);
        var method = typeof(OdometerEndpoints).GetMethod("RecordAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        async Task<int?> Status(int mileage, ClaimsPrincipal principal, int vehicleId = 7) =>
            ((IStatusCodeHttpResult)await (Task<IResult>)method.Invoke(null,
                [vehicleId, new OdometerInput(mileage), principal, service, CancellationToken.None])!).StatusCode;

        Assert.Equal(401, await Status(20000, new ClaimsPrincipal()));
        Assert.Equal(400, await Status(-1, Principal(owner)));
        Assert.Equal(0, service.Calls);
        Assert.Equal(200, await Status(20000, Principal(owner)));
        Assert.Equal(owner, service.LastOwner);
        Assert.Equal(7, service.LastVehicle);
        Assert.Equal(400, await Status(10000, Principal(owner)));
        Assert.Equal(404, await Status(20000, Principal(Guid.NewGuid())));
        Assert.Equal(404, await Status(20000, Principal(owner), 999));
    }

    [Fact]
    public async Task HistoryValidatesPaginationAndOwnership()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingOdometer(owner);
        var method = typeof(OdometerEndpoints).GetMethod("GetHistoryAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        async Task<int?> Status(int? page, int? pageSize, ClaimsPrincipal principal) =>
            ((IStatusCodeHttpResult)await (Task<IResult>)method.Invoke(null,
                [7, page, pageSize, principal, service, CancellationToken.None])!).StatusCode;

        Assert.Equal(401, await Status(1, 10, new ClaimsPrincipal()));
        Assert.Equal(400, await Status(0, 10, Principal(owner)));
        Assert.Equal(400, await Status(1, 101, Principal(owner)));
        Assert.Equal(200, await Status(2, 1, Principal(owner)));
        Assert.Equal(2, service.LastPage);
        Assert.Equal(1, service.LastPageSize);
        Assert.Equal(404, await Status(1, 10, Principal(Guid.NewGuid())));
    }

    [Fact]
    public void UpdatedMileageChangesMaintenanceStatusThroughExistingCalculator()
    {
        var schedule = new MaintenanceSchedule { IntervalKm = 10000 };
        var completion = new MaintenanceCompletion(new DateOnly(2026, 1, 1), 10000);
        var today = new DateOnly(2026, 6, 1);
        var before = MaintenanceDueCalculator.Calculate(schedule, completion, 18000, today);
        var dueSoon = MaintenanceDueCalculator.Calculate(schedule, completion, 19000, today);
        var overdue = MaintenanceDueCalculator.Calculate(schedule, completion, 20000, today);
        Assert.Equal(2000, before.RemainingKm);
        Assert.Equal(MaintenanceDueStatus.DueSoon, dueSoon.Status);
        Assert.Equal(1000, dueSoon.RemainingKm);
        Assert.Equal(MaintenanceDueStatus.Overdue, overdue.Status);
        Assert.Equal(0, overdue.RemainingKm);
    }

    private static ClaimsPrincipal Principal(Guid owner) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, owner.ToString())], "test"));

    private sealed class RecordingOdometer(Guid owner) : IOdometerService
    {
        public int Calls { get; private set; }
        public Guid LastOwner { get; private set; }
        public int LastVehicle { get; private set; }
        public int LastPage { get; private set; }
        public int LastPageSize { get; private set; }

        public Task<OdometerWriteResult> RecordAsync(Guid ownerId, int vehicleId, int mileageKm, CancellationToken cancellationToken)
        {
            Calls++;
            LastOwner = ownerId;
            LastVehicle = vehicleId;
            var status = ownerId != owner || vehicleId == 999 ? OdometerWriteStatus.NotFound :
                mileageKm < 20000 ? OdometerWriteStatus.LowerMileage : OdometerWriteStatus.Success;
            return Task.FromResult(new OdometerWriteResult(status, status == OdometerWriteStatus.Success
                ? new OdometerUpdateDto(1, vehicleId, mileageKm, DateTimeOffset.UtcNow) : null));
        }

        public Task<OdometerHistoryDto?> GetHistoryAsync(Guid ownerId, int vehicleId, int page, int pageSize,
            CancellationToken cancellationToken)
        {
            LastPage = page;
            LastPageSize = pageSize;
            return Task.FromResult<OdometerHistoryDto?>(ownerId == owner
                ? new OdometerHistoryDto([], page, pageSize, 0) : null);
        }
    }
}

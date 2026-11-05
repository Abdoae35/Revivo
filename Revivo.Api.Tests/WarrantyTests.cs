using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Revivo.Api.Warranties;
using Revivo.Application.Warranties;
using Revivo.Domain.Entities;
using Revivo.Domain.Maintenance;
using Revivo.Domain.Warranties;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Api.Tests;

public class WarrantyTests
{
    [Fact]
    public void ExpirationUsesCalendarMonthsAndDefaultsToActive()
    {
        var warranty = new MaintenanceWarranty
        {
            StartDate = new DateOnly(2024, 1, 31), DurationMonths = 1
        };
        var result = WarrantyStatusCalculator.Calculate(warranty, new DateOnly(2024, 1, 31));
        Assert.Equal(new DateOnly(2024, 2, 29), result.ExpirationDate);
        Assert.Equal(29, result.RemainingDays);
        Assert.Equal(WarrantyStatus.ExpiringSoon, result.Status); // Default warning is 30 days.
        Assert.Equal(WarrantyStatus.Active,
            WarrantyStatusCalculator.Calculate(new MaintenanceWarranty
            { StartDate = new DateOnly(2024, 1, 1), DurationMonths = 12 }, new DateOnly(2024, 6, 1)).Status);
    }

    [Fact]
    public void ExpiringSoonAndExpiredRespectWarningDays()
    {
        var warranty = new MaintenanceWarranty
        {
            StartDate = new DateOnly(2024, 1, 1), DurationMonths = 12, WarningDays = 10
        };
        Assert.Equal(WarrantyStatus.Active,
            WarrantyStatusCalculator.Calculate(warranty, new DateOnly(2024, 12, 21)).Status);
        var soon = WarrantyStatusCalculator.Calculate(warranty, new DateOnly(2024, 12, 22));
        Assert.Equal(10, soon.RemainingDays);
        Assert.Equal(WarrantyStatus.ExpiringSoon, soon.Status);
        var expired = WarrantyStatusCalculator.Calculate(warranty, new DateOnly(2025, 1, 2));
        Assert.Equal(-1, expired.RemainingDays);
        Assert.Equal(WarrantyStatus.Expired, expired.Status);
    }

    [Fact]
    public void UpdatingDurationRecalculatesExpiryWithoutChangingMaintenanceSchedule()
    {
        var warranty = new MaintenanceWarranty
        { StartDate = new DateOnly(2024, 1, 1), DurationMonths = 12 };
        var schedule = new MaintenanceSchedule { IntervalMonths = 6 };
        warranty.DurationMonths = 24;
        Assert.Equal(new DateOnly(2026, 1, 1), warranty.ExpirationDate);
        var maintenanceDue = MaintenanceDueCalculator.Calculate(schedule,
            new MaintenanceCompletion(new DateOnly(2024, 1, 1), 1000), 1000, new DateOnly(2024, 2, 1));
        Assert.Equal(new DateOnly(2024, 7, 1), maintenanceDue.NextDueDate);
    }

    [Fact]
    public void InvalidDurationAndStartDateAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaintenanceWarranty
        { StartDate = new DateOnly(2024, 1, 1), DurationMonths = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaintenanceWarranty
        { StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), DurationMonths = 12 });
        var warranty = new MaintenanceWarranty { StartDate = new DateOnly(2024, 1, 1), DurationMonths = 12 };
        Assert.Throws<ArgumentOutOfRangeException>(() => warranty.WarningDays = -1);
    }

    [Fact]
    public void WarrantyMappingIsOneToOneWithMaintenanceRecord()
    {
        var options = new DbContextOptionsBuilder<RevivoDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Trusted_Connection=True")
            .Options;
        using var db = new RevivoDbContext(options);
        var entity = db.Model.FindEntityType(typeof(MaintenanceWarranty))!;
        Assert.Equal("MaintenanceWarranties", entity.GetTableName());
        Assert.Equal(nameof(MaintenanceWarranty.MaintenanceRecordId), entity.FindPrimaryKey()!.Properties.Single().Name);
        var foreignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(typeof(MaintenanceRecord), foreignKey.PrincipalEntityType.ClrType);
        Assert.NotEqual(typeof(MaintenanceSchedule), foreignKey.PrincipalEntityType.ClrType);
    }

    [Fact]
    public async Task UpsertEndpointValidatesAndUsesJwtOwner()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingWarranties(owner);
        var method = typeof(WarrantyEndpoints).GetMethod("UpsertAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var input = new WarrantyInput(12, null, null, "Provider", "Brand", null);
        var created = await (Task<IResult>)method.Invoke(null,
            [7, 3, input, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(201, ((IStatusCodeHttpResult)created).StatusCode);
        Assert.Equal(owner, service.LastOwner);
        Assert.Equal(1, service.Calls);

        var invalid = await (Task<IResult>)method.Invoke(null,
            [7, 3, input with { DurationMonths = 0 }, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(400, ((IStatusCodeHttpResult)invalid).StatusCode);
        Assert.Equal(1, service.Calls);

        service.Exists = true;
        var updated = await (Task<IResult>)method.Invoke(null,
            [7, 3, input with { DurationMonths = 24 }, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(200, ((IStatusCodeHttpResult)updated).StatusCode);
        var missing = await (Task<IResult>)method.Invoke(null,
            [7, 99, input, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)missing).StatusCode);
    }

    [Fact]
    public async Task OtherOwnerCannotReadOrDeleteWarranty()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingWarranties(owner) { Exists = true };
        var get = typeof(WarrantyEndpoints).GetMethod("GetAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var delete = typeof(WarrantyEndpoints).GetMethod("DeleteAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var other = Principal(Guid.NewGuid());
        var forbiddenRead = await (Task<IResult>)get.Invoke(null, [7, 3, other, service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)forbiddenRead).StatusCode);
        var ownedRead = await (Task<IResult>)get.Invoke(null, [7, 3, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(200, ((IStatusCodeHttpResult)ownedRead).StatusCode);
        var forbiddenDelete = await (Task<IResult>)delete.Invoke(null, [7, 3, other, service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)forbiddenDelete).StatusCode);
        var deleted = await (Task<IResult>)delete.Invoke(null, [7, 3, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(204, ((IStatusCodeHttpResult)deleted).StatusCode);
        var missing = await (Task<IResult>)delete.Invoke(null, [7, 3, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)missing).StatusCode);
    }

    [Fact]
    public async Task MissingJwtClaimReturnsUnauthorized()
    {
        var method = typeof(WarrantyEndpoints).GetMethod("GetAllAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = await (Task<IResult>)method.Invoke(null,
            [7, new ClaimsPrincipal(), new RecordingWarranties(Guid.NewGuid()), CancellationToken.None])!;
        Assert.Equal(401, ((IStatusCodeHttpResult)result).StatusCode);
    }

    private static ClaimsPrincipal Principal(Guid owner) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, owner.ToString())], "test"));

    private sealed class RecordingWarranties(Guid owner) : IWarrantyService
    {
        public Guid LastOwner { get; private set; }
        public int Calls { get; private set; }
        public bool Exists { get; set; }
        public Task<WarrantyWriteResult> UpsertAsync(Guid ownerId, int vehicleId, int recordId,
            WarrantyInput input, CancellationToken cancellationToken)
        {
            LastOwner = ownerId;
            Calls++;
            var status = recordId == 99 || ownerId != owner ? WarrantyWriteStatus.RecordNotFound
                : Exists ? WarrantyWriteStatus.Updated : WarrantyWriteStatus.Created;
            return Task.FromResult(new WarrantyWriteResult(status));
        }
        public Task<IReadOnlyList<WarrantyDto>?> GetAllAsync(Guid ownerId, int vehicleId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<WarrantyDto>?>(
                ownerId == owner ? [] : null);
        public Task<WarrantyDto?> GetAsync(Guid ownerId, int vehicleId, int recordId,
            CancellationToken cancellationToken) => Task.FromResult<WarrantyDto?>(
                ownerId == owner && Exists
                    ? new WarrantyDto(recordId, vehicleId, 1, "Battery", 12,
                        new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1), 30, 0,
                        "ExpiringSoon", null, null, null, DateTimeOffset.UtcNow)
                    : null);
        public Task<bool> DeleteAsync(Guid ownerId, int vehicleId, int recordId,
            CancellationToken cancellationToken)
        {
            if (ownerId != owner || !Exists) return Task.FromResult(false);
            Exists = false;
            return Task.FromResult(true);
        }
    }
}

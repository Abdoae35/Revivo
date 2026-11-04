using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Revivo.Api.Maintenance;
using Revivo.Application.Maintenance;
using Revivo.Domain.Entities;
using Revivo.Domain.Maintenance;

namespace Revivo.Api.Tests;

public class PeriodicMaintenanceTests
{
    [Fact]
    public void MileageScheduleUsesLastMileageAndWarningThreshold()
    {
        var schedule = new MaintenanceSchedule { IntervalKm = 10000 };
        var last = new MaintenanceCompletion(new DateOnly(2026, 1, 1), 50000);
        var today = new DateOnly(2026, 6, 1);

        var onTrack = MaintenanceDueCalculator.Calculate(schedule, last, 58000, today);
        Assert.Equal(60000, onTrack.NextDueMileageKm);
        Assert.Equal(2000, onTrack.RemainingKm);
        Assert.Equal(MaintenanceDueStatus.OnTrack, onTrack.Status);
        Assert.Equal(MaintenanceDueStatus.DueSoon,
            MaintenanceDueCalculator.Calculate(schedule, last, 59000, today).Status);
        Assert.Equal(MaintenanceDueStatus.Overdue,
            MaintenanceDueCalculator.Calculate(schedule, last, 60000, today).Status);
    }

    [Fact]
    public void TimeScheduleCalculatesCalendarMonthsAndOverdueDate()
    {
        var schedule = new MaintenanceSchedule { IntervalMonths = 12 };
        var last = new MaintenanceCompletion(new DateOnly(2026, 1, 1), 1000);
        var dueSoon = MaintenanceDueCalculator.Calculate(schedule, last, 1000, new DateOnly(2026, 12, 15));
        Assert.Equal(new DateOnly(2027, 1, 1), dueSoon.NextDueDate);
        Assert.Equal(17, dueSoon.RemainingDays);
        Assert.Equal(MaintenanceDueStatus.DueSoon, dueSoon.Status);
        Assert.Equal(MaintenanceDueStatus.Overdue,
            MaintenanceDueCalculator.Calculate(schedule, last, 1000, new DateOnly(2027, 1, 2)).Status);
    }

    [Fact]
    public void CombinedScheduleIsDueWhenEitherConditionIsMet()
    {
        var schedule = new MaintenanceSchedule { IntervalKm = 60000, IntervalMonths = 36 };
        var last = new MaintenanceCompletion(new DateOnly(2026, 1, 1), 40000);
        var byMileage = MaintenanceDueCalculator.Calculate(schedule, last, 100000, new DateOnly(2027, 1, 1));
        Assert.Equal(new DateOnly(2029, 1, 1), byMileage.NextDueDate);
        Assert.Equal(100000, byMileage.NextDueMileageKm);
        Assert.Equal(MaintenanceDueStatus.Overdue, byMileage.Status);
        var byDate = MaintenanceDueCalculator.Calculate(schedule, last, 50000, new DateOnly(2029, 1, 2));
        Assert.Equal(MaintenanceDueStatus.Overdue, byDate.Status);
        Assert.Equal(MaintenanceDueStatus.DueSoon,
            MaintenanceDueCalculator.Calculate(schedule, last, 99000, new DateOnly(2027, 1, 1)).Status);
    }

    [Fact]
    public void NonPeriodicAndMissingHistoryHaveNoDuePoint()
    {
        var last = new MaintenanceCompletion(new DateOnly(2026, 1, 1), 1000);
        var nonPeriodic = MaintenanceDueCalculator.Calculate(new MaintenanceSchedule(), last, 5000,
            new DateOnly(2026, 6, 1));
        Assert.Null(nonPeriodic.NextDueDate);
        Assert.Null(nonPeriodic.NextDueMileageKm);
        Assert.Equal(MaintenanceDueStatus.OnTrack, nonPeriodic.Status);
        var noHistory = MaintenanceDueCalculator.Calculate(new MaintenanceSchedule { IntervalKm = 5000 },
            null, 5000, new DateOnly(2026, 6, 1));
        Assert.Null(noHistory.NextDueMileageKm);
        Assert.Equal(MaintenanceDueStatus.OnTrack, noHistory.Status);
    }

    [Fact]
    public void CustomWarningThresholdsAreApplied()
    {
        var schedule = new MaintenanceSchedule { IntervalKm = 10000, WarningKm = 250,
            IntervalMonths = 12, WarningDays = 5 };
        var last = new MaintenanceCompletion(new DateOnly(2026, 1, 1), 50000);
        Assert.Equal(MaintenanceDueStatus.OnTrack,
            MaintenanceDueCalculator.Calculate(schedule, last, 59500, new DateOnly(2026, 12, 20)).Status);
        Assert.Equal(MaintenanceDueStatus.DueSoon,
            MaintenanceDueCalculator.Calculate(schedule, last, 59750, new DateOnly(2026, 12, 20)).Status);
        Assert.Equal(MaintenanceDueStatus.DueSoon,
            MaintenanceDueCalculator.Calculate(schedule, last, 59500, new DateOnly(2026, 12, 28)).Status);
    }

    [Fact]
    public void LatestHistoryUsesMaintenanceDateWhenRecordsArriveOutOfOrder()
    {
        var older = new MaintenanceRecord { Id = 3, MaintenanceDate = new DateOnly(2024, 1, 1),
            MileageKm = 10000, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var latest = new MaintenanceRecord { Id = 2, MaintenanceDate = new DateOnly(2025, 1, 1),
            MileageKm = 20000, CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var selected = MaintenanceHistorySelector.Latest([older, latest]);
        Assert.Same(latest, selected);
        Assert.Equal(30000, MaintenanceDueCalculator.Calculate(
            new MaintenanceSchedule { IntervalKm = 10000 },
            new MaintenanceCompletion(selected!.MaintenanceDate, selected.MileageKm), 25000,
            new DateOnly(2025, 6, 1)).NextDueMileageKm);
    }

    [Fact]
    public void ScheduleRejectsInvalidIntervalsAndThresholds()
    {
        var schedule = new MaintenanceSchedule();
        Assert.Throws<ArgumentOutOfRangeException>(() => schedule.IntervalKm = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => schedule.IntervalMonths = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => schedule.WarningKm = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => schedule.WarningDays = -1);
    }

    [Fact]
    public async Task ScheduleEndpointUsesJwtOwnerAndRejectsInvalidIntervals()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingPeriodic(owner);
        var method = typeof(PeriodicMaintenanceEndpoints).GetMethod("ConfigureAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var valid = await (Task<IResult>)method.Invoke(null,
            [7, 2, new ScheduleInput(10000, null, null, null), Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(200, ((IStatusCodeHttpResult)valid).StatusCode);
        Assert.Equal(owner, service.LastOwner);
        Assert.Equal(7, service.LastVehicleId);
        Assert.Equal(1, service.Calls);

        var invalid = await (Task<IResult>)method.Invoke(null,
            [7, 2, new ScheduleInput(0, -1, -1, -1), Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(400, ((IStatusCodeHttpResult)invalid).StatusCode);
        Assert.Equal(1, service.Calls);
        var missing = await (Task<IResult>)method.Invoke(null,
            [7, 99, new ScheduleInput(10000, null, null, null), Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)missing).StatusCode);
    }

    [Fact]
    public async Task StatusEndpointRejectsMissingJwtAndOtherOwnersVehicle()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingPeriodic(owner);
        var method = typeof(PeriodicMaintenanceEndpoints).GetMethod("GetStatusesAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var unauthenticated = await (Task<IResult>)method.Invoke(null,
            [7, new ClaimsPrincipal(), service, CancellationToken.None])!;
        Assert.Equal(401, ((IStatusCodeHttpResult)unauthenticated).StatusCode);
        var otherOwner = await (Task<IResult>)method.Invoke(null,
            [7, Principal(Guid.NewGuid()), service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)otherOwner).StatusCode);
    }

    private static ClaimsPrincipal Principal(Guid owner) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, owner.ToString())], "test"));

    private sealed class RecordingPeriodic(Guid owner) : IPeriodicMaintenanceService
    {
        public Guid LastOwner { get; private set; }
        public int LastVehicleId { get; private set; }
        public int Calls { get; private set; }
        public Task<ScheduleWriteResult> ConfigureAsync(Guid ownerId, int vehicleId, int itemId,
            ScheduleInput input, CancellationToken cancellationToken)
        {
            LastOwner = ownerId;
            LastVehicleId = vehicleId;
            Calls++;
            return Task.FromResult(new ScheduleWriteResult(itemId == 99
                ? ScheduleWriteStatus.ItemNotFound : ScheduleWriteStatus.Success));
        }
        public Task<IReadOnlyList<MaintenanceStatusDto>?> GetStatusesAsync(Guid ownerId, int vehicleId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MaintenanceStatusDto>?>(
                ownerId == owner ? [] : null);
        public Task<IReadOnlyList<MaintenanceStatusDto>?> GetUpcomingAsync(Guid ownerId, int vehicleId,
            CancellationToken cancellationToken) => GetStatusesAsync(ownerId, vehicleId, cancellationToken);
    }
}

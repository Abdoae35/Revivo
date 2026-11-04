using Microsoft.EntityFrameworkCore;
using Revivo.Application.Maintenance;
using Revivo.Domain.Entities;
using Revivo.Domain.Maintenance;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Maintenance;

public sealed class PeriodicMaintenanceService(RevivoDbContext db) : IPeriodicMaintenanceService
{
    public async Task<ScheduleWriteResult> ConfigureAsync(Guid ownerId, int vehicleId, int itemId,
        ScheduleInput input, CancellationToken cancellationToken)
    {
        if (!await db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId,
                cancellationToken)) return new(ScheduleWriteStatus.VehicleNotFound);
        var schedule = await db.MaintenanceSchedules.SingleOrDefaultAsync(
            entry => entry.VehicleId == vehicleId && entry.MaintenanceItemId == itemId, cancellationToken);
        if (schedule is null && !await db.MaintenanceRecords.AnyAsync(record =>
                record.VehicleId == vehicleId && record.MaintenanceItemId == itemId, cancellationToken))
            return new(ScheduleWriteStatus.ItemNotFound);
        if (schedule is null)
        {
            schedule = new MaintenanceSchedule { VehicleId = vehicleId, MaintenanceItemId = itemId };
            db.MaintenanceSchedules.Add(schedule);
        }
        schedule.IntervalKm = input.IntervalKm;
        schedule.IntervalMonths = input.IntervalMonths;
        schedule.WarningKm = input.WarningKm ?? 1000;
        schedule.WarningDays = input.WarningDays ?? 30;
        await db.SaveChangesAsync(cancellationToken);

        var statuses = await GetStatusesAsync(ownerId, vehicleId, cancellationToken);
        return new(ScheduleWriteStatus.Success, statuses!.Single(item => item.ItemId == itemId));
    }

    public async Task<IReadOnlyList<MaintenanceStatusDto>?> GetStatusesAsync(Guid ownerId, int vehicleId,
        CancellationToken cancellationToken)
    {
        var mileage = await db.Vehicles.AsNoTracking()
            .Where(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId)
            .Select(vehicle => (int?)vehicle.CurrentMileageKm).SingleOrDefaultAsync(cancellationToken);
        if (mileage is null) return null;

        var schedules = (await db.MaintenanceSchedules.AsNoTracking()
            .Where(schedule => schedule.VehicleId == vehicleId)
            .Select(schedule => new ScheduleRow(schedule.MaintenanceItemId, schedule.Item.Category.Name,
                schedule.Item.Name, schedule.IntervalKm, schedule.IntervalMonths,
                schedule.WarningKm, schedule.WarningDays)).ToListAsync(cancellationToken))
            .ToDictionary(schedule => schedule.ItemId);

        var history = (await db.MaintenanceRecords.AsNoTracking()
            .Where(record => record.VehicleId == vehicleId)
            .Include(record => record.Item).ThenInclude(item => item.Category)
            .ToListAsync(cancellationToken))
            .GroupBy(record => record.MaintenanceItemId)
            .ToDictionary(group => group.Key, group => MaintenanceHistorySelector.Latest(group)!);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new List<MaintenanceStatusDto>();
        foreach (var itemId in schedules.Keys.Union(history.Keys))
        {
            schedules.TryGetValue(itemId, out var stored);
            history.TryGetValue(itemId, out var last);
            var schedule = stored is null ? null : new MaintenanceSchedule
            {
                IntervalKm = stored.IntervalKm, IntervalMonths = stored.IntervalMonths,
                WarningKm = stored.WarningKm, WarningDays = stored.WarningDays
            };
            var completion = last is null ? null : new MaintenanceCompletion(last.MaintenanceDate, last.MileageKm);
            var due = MaintenanceDueCalculator.Calculate(schedule, completion, mileage.Value, today);
            result.Add(new MaintenanceStatusDto(itemId, stored?.Category ?? last!.Item.Category.Name,
                stored?.ItemName ?? last!.Item.Name, stored?.IntervalKm, stored?.IntervalMonths,
                stored?.WarningKm ?? 1000, stored?.WarningDays ?? 30,
                last?.MaintenanceDate, last?.MileageKm, due.NextDueDate, due.NextDueMileageKm,
                due.RemainingDays, due.RemainingKm, due.Status.ToString()));
        }
        return result.OrderBy(item => item.Category).ThenBy(item => item.ItemName).ToArray();
    }

    public async Task<IReadOnlyList<MaintenanceStatusDto>?> GetUpcomingAsync(Guid ownerId, int vehicleId,
        CancellationToken cancellationToken)
    {
        var all = await GetStatusesAsync(ownerId, vehicleId, cancellationToken);
        return all?.Where(item => item.Status != nameof(MaintenanceDueStatus.OnTrack))
            .OrderBy(item => item.Status == nameof(MaintenanceDueStatus.Overdue) ? 0 : 1)
            .ThenBy(item => item.RemainingDays ?? int.MaxValue)
            .ThenBy(item => item.RemainingKm ?? long.MaxValue).ToArray();
    }

    private sealed record ScheduleRow(int ItemId, string Category, string ItemName,
        int? IntervalKm, int? IntervalMonths, int WarningKm, int WarningDays);
}

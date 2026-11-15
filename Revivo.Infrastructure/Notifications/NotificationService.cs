using Microsoft.EntityFrameworkCore;
using Revivo.Application.Maintenance;
using Revivo.Application.Notifications;
using Revivo.Application.Warranties;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;
using Revivo.Infrastructure.Vehicles;

namespace Revivo.Infrastructure.Notifications;

public sealed class NotificationService(RevivoDbContext db, IPeriodicMaintenanceService maintenance,
    IWarrantyService warranties) : INotificationService
{
    public async Task<NotificationSettingsDto?> GetSettingsAsync(Guid userId, int vehicleId,
        CancellationToken cancellationToken)
    {
        if (!await VehicleAccess.CanReadAsync(db, userId, vehicleId, cancellationToken)) return null;
        var settings = await db.NotificationSettings.AsNoTracking().SingleOrDefaultAsync(
            item => item.UserId == userId && item.VehicleId == vehicleId, cancellationToken);
        return ToDto(settings ?? Defaults(userId, vehicleId));
    }

    public async Task<NotificationSettingsDto?> UpdateSettingsAsync(Guid userId, int vehicleId,
        NotificationSettingsInput input, CancellationToken cancellationToken)
    {
        if (!await OwnsVehicleAsync(userId, vehicleId, cancellationToken)) return null;
        var settings = await db.NotificationSettings.SingleOrDefaultAsync(
            item => item.UserId == userId && item.VehicleId == vehicleId, cancellationToken);
        if (settings is null)
        {
            settings = Defaults(userId, vehicleId);
            db.NotificationSettings.Add(settings);
        }
        settings.WeeklyReminderEnabled = input.WeeklyReminderEnabled;
        settings.ReminderDayOfWeek = input.ReminderDayOfWeek!.Value;
        settings.ReminderTime = input.ReminderTime!.Value;
        settings.TimeZoneId = input.TimeZoneId!;
        settings.MaintenanceAlertsEnabled = input.MaintenanceAlertsEnabled;
        settings.WarrantyAlertsEnabled = input.WarrantyAlertsEnabled;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<VehicleAlertsDto?> GetAlertsAsync(Guid userId, int vehicleId,
        CancellationToken cancellationToken)
    {
        var settings = await GetSettingsAsync(userId, vehicleId, cancellationToken);
        if (settings is null) return null;

        IReadOnlyList<MaintenanceStatusDto> maintenanceStatuses = settings.MaintenanceAlertsEnabled
            ? await maintenance.GetUpcomingAsync(userId, vehicleId, cancellationToken) ?? [] : [];
        IReadOnlyList<WarrantyDto> warrantyStatuses = settings.WarrantyAlertsEnabled
            ? await warranties.GetAllAsync(userId, vehicleId, cancellationToken) ?? [] : [];
        return NotificationAlertSelector.Select(vehicleId, settings.MaintenanceAlertsEnabled,
            settings.WarrantyAlertsEnabled, maintenanceStatuses, warrantyStatuses);
    }

    private Task<bool> OwnsVehicleAsync(Guid userId, int vehicleId, CancellationToken cancellationToken) =>
        db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == userId, cancellationToken);

    private static NotificationSettings Defaults(Guid userId, int vehicleId) =>
        new() { UserId = userId, VehicleId = vehicleId };

    private static NotificationSettingsDto ToDto(NotificationSettings settings) =>
        new(settings.Id, settings.VehicleId, settings.UserId, settings.WeeklyReminderEnabled,
            settings.ReminderDayOfWeek, settings.ReminderTime, settings.TimeZoneId,
            settings.MaintenanceAlertsEnabled, settings.WarrantyAlertsEnabled);
}

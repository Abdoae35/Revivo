using Revivo.Application.Maintenance;
using Revivo.Application.Warranties;

namespace Revivo.Application.Notifications;

public sealed record NotificationSettingsInput(bool WeeklyReminderEnabled, int? ReminderDayOfWeek,
    TimeOnly? ReminderTime, string? TimeZoneId, bool MaintenanceAlertsEnabled, bool WarrantyAlertsEnabled);

public sealed record NotificationSettingsDto(int Id, int VehicleId, Guid UserId, bool WeeklyReminderEnabled,
    int ReminderDayOfWeek, TimeOnly ReminderTime, string TimeZoneId, bool MaintenanceAlertsEnabled,
    bool WarrantyAlertsEnabled);

public sealed record VehicleAlertsDto(int VehicleId, IReadOnlyList<MaintenanceStatusDto> Maintenance,
    IReadOnlyList<WarrantyDto> Warranties);

public interface INotificationService
{
    Task<NotificationSettingsDto?> GetSettingsAsync(Guid userId, int vehicleId, CancellationToken cancellationToken);
    Task<NotificationSettingsDto?> UpdateSettingsAsync(Guid userId, int vehicleId,
        NotificationSettingsInput input, CancellationToken cancellationToken);
    Task<VehicleAlertsDto?> GetAlertsAsync(Guid userId, int vehicleId, CancellationToken cancellationToken);
}

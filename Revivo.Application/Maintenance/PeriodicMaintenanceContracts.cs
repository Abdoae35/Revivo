namespace Revivo.Application.Maintenance;

public sealed record ScheduleInput(int? IntervalKm, int? IntervalMonths, int? WarningKm, int? WarningDays);

public sealed record MaintenanceStatusDto(
    int ItemId, string Category, string ItemName, int? IntervalKm, int? IntervalMonths,
    int WarningKm, int WarningDays, DateOnly? LastMaintenanceDate, int? LastMaintenanceMileage,
    DateOnly? NextDueDate, long? NextDueMileageKm, int? RemainingDays, long? RemainingKm, string Status);

public enum ScheduleWriteStatus { Success, VehicleNotFound, ItemNotFound }
public sealed record ScheduleWriteResult(ScheduleWriteStatus Status, MaintenanceStatusDto? Item = null);

public interface IPeriodicMaintenanceService
{
    Task<ScheduleWriteResult> ConfigureAsync(Guid ownerId, int vehicleId, int itemId,
        ScheduleInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceStatusDto>?> GetStatusesAsync(Guid ownerId, int vehicleId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceStatusDto>?> GetUpcomingAsync(Guid ownerId, int vehicleId,
        CancellationToken cancellationToken);
}

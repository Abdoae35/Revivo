namespace Revivo.Application.Maintenance;

public sealed record MaintenanceInput(
    string? Category, string? ItemName, DateOnly MaintenanceDate, int MileageKm, decimal CostEgp, string? Notes);

public sealed record MaintenanceDto(
    int Id, int VehicleId, int ItemId, string Category, string ItemName, DateOnly MaintenanceDate,
    int MileageKm, decimal CostEgp, string? Notes, DateTimeOffset CreatedAt);

public sealed record MaintenancePage(int Page, int PageSize, int TotalCount, IReadOnlyList<MaintenanceDto> Items);

public enum MaintenanceWriteStatus { Success, VehicleNotFound, RecordNotFound, InvalidDate }
public sealed record MaintenanceWriteResult(MaintenanceWriteStatus Status, MaintenanceDto? Record = null);

public interface IMaintenanceService
{
    Task<MaintenanceWriteResult> CreateAsync(Guid ownerId, int vehicleId, MaintenanceInput input, CancellationToken cancellationToken);
    Task<MaintenancePage?> GetPageAsync(Guid ownerId, int vehicleId, int page, int pageSize, CancellationToken cancellationToken);
    Task<MaintenanceDto?> GetAsync(Guid ownerId, int vehicleId, int id, CancellationToken cancellationToken);
    Task<MaintenanceWriteResult> UpdateAsync(Guid ownerId, int vehicleId, int id, MaintenanceInput input, CancellationToken cancellationToken);
    Task<MaintenanceWriteStatus> DeleteAsync(Guid ownerId, int vehicleId, int id, CancellationToken cancellationToken);
}

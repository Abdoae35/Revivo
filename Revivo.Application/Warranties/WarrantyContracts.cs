namespace Revivo.Application.Warranties;

public sealed record WarrantyInput(int DurationMonths, DateOnly? StartDate,
    int? WarningDays, string? Provider, string? Brand, string? Notes);

public sealed record WarrantyDto(int MaintenanceRecordId, int VehicleId, int ItemId, string ItemName,
    int DurationMonths, DateOnly StartDate, DateOnly ExpirationDate, int WarningDays,
    int RemainingDays, string Status, string? Provider, string? Brand, string? Notes,
    DateTimeOffset CreatedAt);

public enum WarrantyWriteStatus { Created, Updated, RecordNotFound }
public sealed record WarrantyWriteResult(WarrantyWriteStatus Status, WarrantyDto? Warranty = null);

public interface IWarrantyService
{
    Task<WarrantyWriteResult> UpsertAsync(Guid ownerId, int vehicleId, int recordId,
        WarrantyInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<WarrantyDto>?> GetAllAsync(Guid ownerId, int vehicleId, CancellationToken cancellationToken);
    Task<WarrantyDto?> GetAsync(Guid ownerId, int vehicleId, int recordId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid ownerId, int vehicleId, int recordId, CancellationToken cancellationToken);
}

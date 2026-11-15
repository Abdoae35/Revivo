namespace Revivo.Application.Vehicles;

public sealed record OdometerInput(int MileageKm);
public sealed record OdometerUpdateDto(int ReadingId, int VehicleId, int CurrentMileageKm, DateTimeOffset RecordedAt);
public sealed record OdometerReadingDto(int Id, int VehicleId, int MileageKm, DateTimeOffset RecordedAt, Guid RecordedByUserId);
public sealed record OdometerHistoryDto(IReadOnlyList<OdometerReadingDto> Items, int Page, int PageSize, int TotalCount);
public enum OdometerWriteStatus { Success, NotFound, LowerMileage }
public sealed record OdometerWriteResult(OdometerWriteStatus Status, OdometerUpdateDto? Update = null);

public interface IOdometerService
{
    Task<OdometerWriteResult> RecordAsync(Guid ownerId, int vehicleId, int mileageKm, CancellationToken cancellationToken);
    Task<OdometerHistoryDto?> GetHistoryAsync(Guid ownerId, int vehicleId, int page, int pageSize, CancellationToken cancellationToken);
}

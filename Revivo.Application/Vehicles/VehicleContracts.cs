namespace Revivo.Application.Vehicles;

public sealed record CarMakeDto(int Id, string Name);
public sealed record CarModelDto(int Id, int CarMakeId, string Name, int? YearStart, int? YearEnd);
public sealed record CarImageModelDto(string Make, string Model, int? YearStart, int? YearEnd);
public sealed record VehicleDto(int Id, Guid OwnerId, int CarModelId, int Year, int CurrentMileageKm,
    string? Nickname, string? ImageUrl, DateTimeOffset CreatedAt);
public sealed record VehicleInput(int CarModelId, int Year, int CurrentMileageKm, string? Nickname, string? ImageUrl);

public interface ICarCatalogService
{
    Task<IReadOnlyList<CarMakeDto>> GetMakesAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId, string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<int>> GetYearsAsync(int modelId, CancellationToken cancellationToken);
    Task<CarImageModelDto?> GetModelForImageAsync(int modelId, CancellationToken cancellationToken);
}

public enum VehicleWriteStatus { Success, NotFound, InvalidModel }
public sealed record VehicleWriteResult(VehicleWriteStatus Status, VehicleDto? Vehicle = null);

public interface IVehicleService
{
    Task<VehicleWriteResult> CreateAsync(Guid ownerId, VehicleInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleDto>> GetAllAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<VehicleDto?> GetAsync(Guid ownerId, int id, CancellationToken cancellationToken);
    Task<VehicleWriteResult> UpdateAsync(Guid ownerId, int id, VehicleInput input, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid ownerId, int id, CancellationToken cancellationToken);
}

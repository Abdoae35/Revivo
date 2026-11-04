using Microsoft.EntityFrameworkCore;
using Revivo.Application.Vehicles;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Vehicles;

public sealed class VehicleService(RevivoDbContext db) : IVehicleService
{
    public async Task<VehicleWriteResult> CreateAsync(Guid ownerId, VehicleInput input, CancellationToken cancellationToken)
    {
        if (!await db.CarModels.AnyAsync(model => model.Id == input.CarModelId, cancellationToken))
            return new(VehicleWriteStatus.InvalidModel);
        var vehicle = new Vehicle
        {
            OwnerId = ownerId, CarModelId = input.CarModelId, Year = input.Year,
            CurrentMileageKm = input.CurrentMileageKm, Nickname = input.Nickname, ImageUrl = input.ImageUrl
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        return new(VehicleWriteStatus.Success, ToDto(vehicle));
    }

    public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.Vehicles.AsNoTracking().Where(vehicle => vehicle.OwnerId == ownerId)
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .Select(vehicle => new VehicleDto(vehicle.Id, vehicle.OwnerId, vehicle.CarModelId, vehicle.Year,
                vehicle.CurrentMileageKm, vehicle.Nickname, vehicle.ImageUrl, vehicle.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<VehicleDto?> GetAsync(Guid ownerId, int id, CancellationToken cancellationToken) =>
        await db.Vehicles.AsNoTracking().Where(vehicle => vehicle.OwnerId == ownerId && vehicle.Id == id)
            .Select(vehicle => new VehicleDto(vehicle.Id, vehicle.OwnerId, vehicle.CarModelId, vehicle.Year,
                vehicle.CurrentMileageKm, vehicle.Nickname, vehicle.ImageUrl, vehicle.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<VehicleWriteResult> UpdateAsync(Guid ownerId, int id, VehicleInput input, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(item => item.OwnerId == ownerId && item.Id == id, cancellationToken);
        if (vehicle is null) return new(VehicleWriteStatus.NotFound);
        if (!await db.CarModels.AnyAsync(model => model.Id == input.CarModelId, cancellationToken))
            return new(VehicleWriteStatus.InvalidModel);
        var highestMaintenanceMileage = await db.MaintenanceRecords
            .Where(record => record.VehicleId == id).MaxAsync(record => (int?)record.MileageKm, cancellationToken);
        if (input.CurrentMileageKm < highestMaintenanceMileage)
            return new(VehicleWriteStatus.MileageBelowMaintenance);
        vehicle.CarModelId = input.CarModelId;
        vehicle.Year = input.Year;
        vehicle.CurrentMileageKm = input.CurrentMileageKm;
        vehicle.Nickname = input.Nickname;
        vehicle.ImageUrl = input.ImageUrl;
        await db.SaveChangesAsync(cancellationToken);
        return new(VehicleWriteStatus.Success, ToDto(vehicle));
    }

    public async Task<VehicleDeleteStatus> DeleteAsync(Guid ownerId, int id, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(item => item.OwnerId == ownerId && item.Id == id, cancellationToken);
        if (vehicle is null) return VehicleDeleteStatus.NotFound;
        if (await db.MaintenanceRecords.AnyAsync(record => record.VehicleId == id, cancellationToken))
            return VehicleDeleteStatus.HasMaintenance;
        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        return VehicleDeleteStatus.Deleted;
    }

    private static VehicleDto ToDto(Vehicle vehicle) => new(vehicle.Id, vehicle.OwnerId, vehicle.CarModelId,
        vehicle.Year, vehicle.CurrentMileageKm, vehicle.Nickname, vehicle.ImageUrl, vehicle.CreatedAt);
}

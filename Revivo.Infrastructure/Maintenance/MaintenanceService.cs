using Microsoft.EntityFrameworkCore;
using Revivo.Application.Maintenance;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Maintenance;

public sealed class MaintenanceService(RevivoDbContext db) : IMaintenanceService
{
    public async Task<MaintenanceWriteResult> CreateAsync(Guid ownerId, int vehicleId, MaintenanceInput input,
        CancellationToken cancellationToken)
    {
        var vehicle = await OwnedVehicleAsync(ownerId, vehicleId, cancellationToken);
        if (vehicle is null) return new(MaintenanceWriteStatus.VehicleNotFound);
        if (input.MaintenanceDate.Year < vehicle.Year) return new(MaintenanceWriteStatus.InvalidDate);

        var item = await GetOrCreateItemAsync(input.Category!.Trim(), input.ItemName!.Trim(), cancellationToken);
        var record = new MaintenanceRecord
        {
            VehicleId = vehicleId, Item = item, MaintenanceDate = input.MaintenanceDate,
            MileageKm = input.MileageKm, CostEgp = input.CostEgp, Notes = input.Notes?.Trim()
        };
        db.MaintenanceRecords.Add(record);
        vehicle.CurrentMileageKm = Math.Max(vehicle.CurrentMileageKm, record.MileageKm);
        await db.SaveChangesAsync(cancellationToken);
        return new(MaintenanceWriteStatus.Success, ToDto(record, item.Category.Name, item.Name));
    }

    public async Task<MaintenancePage?> GetPageAsync(Guid ownerId, int vehicleId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId, cancellationToken))
            return null;
        var query = db.MaintenanceRecords.AsNoTracking().Where(record => record.VehicleId == vehicleId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(record => record.MaintenanceDate)
            .ThenByDescending(record => record.CreatedAt).ThenByDescending(record => record.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(record => new MaintenanceDto(record.Id, record.VehicleId, record.MaintenanceItemId, record.Item.Category.Name,
                record.Item.Name, record.MaintenanceDate, record.MileageKm, record.CostEgp,
                record.Notes, record.CreatedAt)).ToListAsync(cancellationToken);
        return new MaintenancePage(page, pageSize, total, items);
    }

    public async Task<MaintenanceDto?> GetAsync(Guid ownerId, int vehicleId, int id, CancellationToken cancellationToken) =>
        await db.MaintenanceRecords.AsNoTracking()
            .Where(record => record.Id == id && record.VehicleId == vehicleId && record.Vehicle.OwnerId == ownerId)
            .Select(record => new MaintenanceDto(record.Id, record.VehicleId, record.MaintenanceItemId, record.Item.Category.Name,
                record.Item.Name, record.MaintenanceDate, record.MileageKm, record.CostEgp,
                record.Notes, record.CreatedAt)).SingleOrDefaultAsync(cancellationToken);

    public async Task<MaintenanceWriteResult> UpdateAsync(Guid ownerId, int vehicleId, int id,
        MaintenanceInput input, CancellationToken cancellationToken)
    {
        var vehicle = await OwnedVehicleAsync(ownerId, vehicleId, cancellationToken);
        if (vehicle is null) return new(MaintenanceWriteStatus.VehicleNotFound);
        var record = await db.MaintenanceRecords.SingleOrDefaultAsync(
            entry => entry.Id == id && entry.VehicleId == vehicleId, cancellationToken);
        if (record is null) return new(MaintenanceWriteStatus.RecordNotFound);
        if (input.MaintenanceDate.Year < vehicle.Year) return new(MaintenanceWriteStatus.InvalidDate);

        var item = await GetOrCreateItemAsync(input.Category!.Trim(), input.ItemName!.Trim(), cancellationToken);
        record.Item = item;
        record.MaintenanceDate = input.MaintenanceDate;
        record.MileageKm = input.MileageKm;
        record.CostEgp = input.CostEgp;
        record.Notes = input.Notes?.Trim();
        vehicle.CurrentMileageKm = Math.Max(vehicle.CurrentMileageKm, record.MileageKm);
        await db.SaveChangesAsync(cancellationToken);
        return new(MaintenanceWriteStatus.Success, ToDto(record, item.Category.Name, item.Name));
    }

    public async Task<MaintenanceWriteStatus> DeleteAsync(Guid ownerId, int vehicleId, int id,
        CancellationToken cancellationToken)
    {
        if (!await db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId, cancellationToken))
            return MaintenanceWriteStatus.VehicleNotFound;
        var record = await db.MaintenanceRecords.SingleOrDefaultAsync(
            entry => entry.Id == id && entry.VehicleId == vehicleId, cancellationToken);
        if (record is null) return MaintenanceWriteStatus.RecordNotFound;
        db.MaintenanceRecords.Remove(record);
        await db.SaveChangesAsync(cancellationToken);
        return MaintenanceWriteStatus.Success;
    }

    private Task<Vehicle?> OwnedVehicleAsync(Guid ownerId, int vehicleId, CancellationToken cancellationToken) =>
        db.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId,
            cancellationToken);

    private async Task<MaintenanceItem> GetOrCreateItemAsync(string categoryName, string itemName,
        CancellationToken cancellationToken)
    {
        var category = await db.MaintenanceCategories.SingleOrDefaultAsync(
            entry => entry.Name == categoryName, cancellationToken);
        if (category is null)
        {
            category = new MaintenanceCategory { Name = categoryName };
            db.MaintenanceCategories.Add(category);
        }
        MaintenanceItem? item = null;
        if (category.Id != 0)
            item = await db.MaintenanceItems.SingleOrDefaultAsync(
                entry => entry.MaintenanceCategoryId == category.Id && entry.Name == itemName, cancellationToken);
        if (item is not null)
        {
            item.Category = category;
            return item;
        }
        item = new MaintenanceItem { Category = category, Name = itemName };
        db.MaintenanceItems.Add(item);
        return item;
    }

    private static MaintenanceDto ToDto(MaintenanceRecord record, string category, string itemName) =>
        new(record.Id, record.VehicleId, record.MaintenanceItemId, category, itemName, record.MaintenanceDate,
            record.MileageKm, record.CostEgp, record.Notes, record.CreatedAt);
}

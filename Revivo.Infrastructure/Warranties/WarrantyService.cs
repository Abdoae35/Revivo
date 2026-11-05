using Microsoft.EntityFrameworkCore;
using Revivo.Application.Warranties;
using Revivo.Domain.Entities;
using Revivo.Domain.Warranties;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Warranties;

public sealed class WarrantyService(RevivoDbContext db) : IWarrantyService
{
    public async Task<WarrantyWriteResult> UpsertAsync(Guid ownerId, int vehicleId, int recordId,
        WarrantyInput input, CancellationToken cancellationToken)
    {
        var record = await db.MaintenanceRecords
            .Include(entry => entry.Item)
            .SingleOrDefaultAsync(entry => entry.Id == recordId && entry.VehicleId == vehicleId &&
                entry.Vehicle.OwnerId == ownerId, cancellationToken);
        if (record is null) return new(WarrantyWriteStatus.RecordNotFound);

        var warranty = await db.MaintenanceWarranties.SingleOrDefaultAsync(
            entry => entry.MaintenanceRecordId == recordId, cancellationToken);
        var created = warranty is null;
        if (warranty is null)
        {
            warranty = new MaintenanceWarranty
            {
                MaintenanceRecordId = recordId,
                DurationMonths = input.DurationMonths,
                StartDate = input.StartDate ?? record.MaintenanceDate
            };
            db.MaintenanceWarranties.Add(warranty);
        }
        else
        {
            warranty.DurationMonths = input.DurationMonths;
            warranty.StartDate = input.StartDate ?? record.MaintenanceDate;
        }
        warranty.WarningDays = input.WarningDays ?? 30;
        warranty.Provider = input.Provider?.Trim();
        warranty.Brand = input.Brand?.Trim();
        warranty.Notes = input.Notes?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return new(created ? WarrantyWriteStatus.Created : WarrantyWriteStatus.Updated, ToDto(warranty, record));
    }

    public async Task<IReadOnlyList<WarrantyDto>?> GetAllAsync(Guid ownerId, int vehicleId,
        CancellationToken cancellationToken)
    {
        if (!await db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId,
                cancellationToken)) return null;
        var warranties = await db.MaintenanceWarranties.AsNoTracking()
            .Where(warranty => warranty.Record.VehicleId == vehicleId &&
                warranty.Record.Vehicle.OwnerId == ownerId)
            .Include(warranty => warranty.Record).ThenInclude(record => record.Item)
            .ToListAsync(cancellationToken);
        return warranties.Select(warranty => ToDto(warranty, warranty.Record))
            .OrderBy(warranty => warranty.ExpirationDate).ThenBy(warranty => warranty.MaintenanceRecordId).ToArray();
    }

    public async Task<WarrantyDto?> GetAsync(Guid ownerId, int vehicleId, int recordId,
        CancellationToken cancellationToken)
    {
        var warranty = await db.MaintenanceWarranties.AsNoTracking()
            .Where(entry => entry.MaintenanceRecordId == recordId && entry.Record.VehicleId == vehicleId &&
                entry.Record.Vehicle.OwnerId == ownerId)
            .Include(entry => entry.Record).ThenInclude(record => record.Item)
            .SingleOrDefaultAsync(cancellationToken);
        return warranty is null ? null : ToDto(warranty, warranty.Record);
    }

    public async Task<bool> DeleteAsync(Guid ownerId, int vehicleId, int recordId,
        CancellationToken cancellationToken)
    {
        var warranty = await db.MaintenanceWarranties.SingleOrDefaultAsync(entry =>
            entry.MaintenanceRecordId == recordId && entry.Record.VehicleId == vehicleId &&
            entry.Record.Vehicle.OwnerId == ownerId, cancellationToken);
        if (warranty is null) return false;
        db.MaintenanceWarranties.Remove(warranty);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WarrantyDto ToDto(MaintenanceWarranty warranty, MaintenanceRecord record)
    {
        var assessment = WarrantyStatusCalculator.Calculate(warranty, DateOnly.FromDateTime(DateTime.UtcNow));
        return new WarrantyDto(record.Id, record.VehicleId, record.MaintenanceItemId, record.Item.Name,
            warranty.DurationMonths, warranty.StartDate, assessment.ExpirationDate, warranty.WarningDays,
            assessment.RemainingDays, assessment.Status.ToString(), warranty.Provider, warranty.Brand,
            warranty.Notes, warranty.CreatedAt);
    }
}

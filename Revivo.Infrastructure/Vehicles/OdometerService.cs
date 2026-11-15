using Microsoft.EntityFrameworkCore;
using Revivo.Application.Vehicles;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Vehicles;

public sealed class OdometerService(RevivoDbContext db) : IOdometerService
{
    public async Task<OdometerWriteResult> RecordAsync(Guid ownerId, int vehicleId, int mileageKm,
        CancellationToken cancellationToken)
    {
        if (mileageKm < 0) throw new ArgumentOutOfRangeException(nameof(mileageKm));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var updated = await db.Vehicles.Where(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId &&
                vehicle.CurrentMileageKm <= mileageKm)
            .ExecuteUpdateAsync(setters => setters.SetProperty(vehicle => vehicle.CurrentMileageKm, mileageKm), cancellationToken);
        if (updated == 0)
        {
            var exists = await db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId,
                cancellationToken);
            return new(exists ? OdometerWriteStatus.LowerMileage : OdometerWriteStatus.NotFound);
        }

        var reading = new OdometerReading
        {
            VehicleId = vehicleId, MileageKm = mileageKm, RecordedByUserId = ownerId,
            RecordedAt = DateTimeOffset.UtcNow
        };
        db.OdometerReadings.Add(reading);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(OdometerWriteStatus.Success,
            new OdometerUpdateDto(reading.Id, vehicleId, mileageKm, reading.RecordedAt));
    }

    public async Task<OdometerHistoryDto?> GetHistoryAsync(Guid ownerId, int vehicleId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await VehicleAccess.CanReadAsync(db, ownerId, vehicleId, cancellationToken)) return null;
        var readings = db.OdometerReadings.AsNoTracking().Where(reading => reading.VehicleId == vehicleId);
        var total = await readings.CountAsync(cancellationToken);
        var items = await readings.OrderByDescending(reading => reading.RecordedAt)
            .ThenByDescending(reading => reading.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(reading => new OdometerReadingDto(reading.Id, reading.VehicleId, reading.MileageKm,
                reading.RecordedAt, reading.RecordedByUserId)).ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }
}

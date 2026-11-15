using Microsoft.EntityFrameworkCore;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Vehicles;

public static class VehicleAccess
{
    public static Task<bool> CanReadAsync(RevivoDbContext db, Guid userId, int vehicleId,
        CancellationToken cancellationToken) =>
        db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId &&
            (vehicle.OwnerId == userId || db.VehicleShares.Any(share =>
                share.VehicleId == vehicle.Id && share.InvitedUserId == userId &&
                share.Status == VehicleShareStatus.Accepted)), cancellationToken);
}

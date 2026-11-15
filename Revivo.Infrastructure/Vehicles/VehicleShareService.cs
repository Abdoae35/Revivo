using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Revivo.Application.Vehicles;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Vehicles;

public sealed class VehicleShareService(RevivoDbContext db) : IVehicleShareService
{
    public async Task<InviteResult> InviteAsync(Guid ownerId, int vehicleId, string email,
        CancellationToken cancellationToken)
    {
        if (!await IsOwnerAsync(ownerId, vehicleId, cancellationToken)) return new(InviteStatus.VehicleNotFound);
        var normalized = email.Trim().ToUpperInvariant();
        var invited = await db.Users.AsNoTracking().SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalized, cancellationToken);
        if (invited is null) return new(InviteStatus.UserNotFound);
        if (invited.Id == ownerId) return new(InviteStatus.SelfInvite);
        if (await db.VehicleShares.AnyAsync(share => share.VehicleId == vehicleId &&
            share.InvitedUserId == invited.Id && share.Status != VehicleShareStatus.Rejected, cancellationToken))
            return new(InviteStatus.Duplicate);

        var share = new VehicleShare
        {
            VehicleId = vehicleId, InvitedUserId = invited.Id, InvitedByUserId = ownerId
        };
        db.VehicleShares.Add(share);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException error) when (error.InnerException is SqlException sql &&
            sql.Number is 2601 or 2627)
        {
            db.Entry(share).State = EntityState.Detached;
            return new(InviteStatus.Duplicate);
        }
        return new(InviteStatus.Created, ToDto(share, invited.Email!));
    }

    public async Task<IReadOnlyList<VehicleShareDto>> GetInvitationsAsync(Guid userId,
        CancellationToken cancellationToken) =>
        await ShareDtos(db.VehicleShares.AsNoTracking().Where(share =>
                share.InvitedUserId == userId && share.Status == VehicleShareStatus.Pending)
            .OrderByDescending(share => share.CreatedAt).ThenByDescending(share => share.Id))
            .ToListAsync(cancellationToken);

    public async Task<InvitationResponseResult> RespondAsync(Guid userId, int shareId, bool accept,
        CancellationToken cancellationToken)
    {
        var share = await db.VehicleShares.SingleOrDefaultAsync(
            item => item.Id == shareId && item.InvitedUserId == userId, cancellationToken);
        if (share is null) return new(InvitationResponseStatus.NotFound);
        if (share.Status != VehicleShareStatus.Pending) return new(InvitationResponseStatus.NotPending);
        share.Respond(userId, accept, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        var email = await db.Users.Where(user => user.Id == userId)
            .Select(user => user.Email).SingleAsync(cancellationToken);
        return new(InvitationResponseStatus.Updated, ToDto(share, email!));
    }

    public async Task<IReadOnlyList<VehicleShareDto>?> GetVehicleSharesAsync(Guid ownerId, int vehicleId,
        CancellationToken cancellationToken)
    {
        if (!await IsOwnerAsync(ownerId, vehicleId, cancellationToken)) return null;
        return await ShareDtos(db.VehicleShares.AsNoTracking().Where(share =>
                share.VehicleId == vehicleId && share.Status != VehicleShareStatus.Rejected)
            .OrderByDescending(share => share.CreatedAt).ThenByDescending(share => share.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid ownerId, int vehicleId, int shareId,
        CancellationToken cancellationToken)
    {
        if (!await IsOwnerAsync(ownerId, vehicleId, cancellationToken)) return false;
        var share = await db.VehicleShares.SingleOrDefaultAsync(item => item.Id == shareId &&
            item.VehicleId == vehicleId && item.Status != VehicleShareStatus.Rejected, cancellationToken);
        if (share is null) return false;
        db.VehicleShares.Remove(share);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<VehicleDto>> GetSharedVehiclesAsync(Guid userId,
        CancellationToken cancellationToken) =>
        await db.Vehicles.AsNoTracking().Where(vehicle => db.VehicleShares.Any(share =>
                share.VehicleId == vehicle.Id && share.InvitedUserId == userId &&
                share.Status == VehicleShareStatus.Accepted))
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .Select(vehicle => new VehicleDto(vehicle.Id, vehicle.OwnerId, vehicle.CarModelId, vehicle.Year,
                vehicle.CurrentMileageKm, vehicle.Nickname, vehicle.ImageUrl, vehicle.CreatedAt))
            .ToListAsync(cancellationToken);

    private Task<bool> IsOwnerAsync(Guid ownerId, int vehicleId, CancellationToken cancellationToken) =>
        db.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.OwnerId == ownerId, cancellationToken);

    private IQueryable<VehicleShareDto> ShareDtos(IQueryable<VehicleShare> shares) =>
        shares.Join(db.Users.AsNoTracking(), share => share.InvitedUserId, user => user.Id,
            (share, user) => new VehicleShareDto(share.Id, share.VehicleId, share.InvitedUserId,
                user.Email!, share.InvitedByUserId, share.Status.ToString(), share.CreatedAt, share.RespondedAt));

    private static VehicleShareDto ToDto(VehicleShare share, string email) =>
        new(share.Id, share.VehicleId, share.InvitedUserId, email, share.InvitedByUserId,
            share.Status.ToString(), share.CreatedAt, share.RespondedAt);
}

namespace Revivo.Domain.Entities;

public enum VehicleShareStatus { Pending, Accepted, Rejected }

public sealed class VehicleShare
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid InvitedUserId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public VehicleShareStatus Status { get; private set; } = VehicleShareStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; private set; }

    public void Respond(Guid userId, bool accept, DateTimeOffset now)
    {
        if (userId != InvitedUserId) throw new UnauthorizedAccessException("Only the invited user can respond.");
        if (Status != VehicleShareStatus.Pending) throw new InvalidOperationException("Invitation is no longer pending.");
        Status = accept ? VehicleShareStatus.Accepted : VehicleShareStatus.Rejected;
        RespondedAt = now;
    }
}

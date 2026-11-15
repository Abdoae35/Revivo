namespace Revivo.Application.Vehicles;

public sealed record VehicleInviteInput(string? Email);
public sealed record VehicleShareDto(int Id, int VehicleId, Guid InvitedUserId, string InvitedUserEmail,
    Guid InvitedByUserId, string Status, DateTimeOffset CreatedAt, DateTimeOffset? RespondedAt);
public enum InviteStatus { Created, VehicleNotFound, UserNotFound, SelfInvite, Duplicate }
public sealed record InviteResult(InviteStatus Status, VehicleShareDto? Share = null);
public enum InvitationResponseStatus { Updated, NotFound, NotPending }
public sealed record InvitationResponseResult(InvitationResponseStatus Status, VehicleShareDto? Share = null);

public interface IVehicleShareService
{
    Task<InviteResult> InviteAsync(Guid ownerId, int vehicleId, string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleShareDto>> GetInvitationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<InvitationResponseResult> RespondAsync(Guid userId, int shareId, bool accept,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleShareDto>?> GetVehicleSharesAsync(Guid ownerId, int vehicleId,
        CancellationToken cancellationToken);
    Task<bool> RevokeAsync(Guid ownerId, int vehicleId, int shareId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleDto>> GetSharedVehiclesAsync(Guid userId, CancellationToken cancellationToken);
}

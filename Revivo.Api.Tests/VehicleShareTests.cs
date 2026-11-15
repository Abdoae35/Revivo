using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Revivo.Api.Vehicles;
using Revivo.Application.Vehicles;
using Revivo.Domain.Entities;

namespace Revivo.Api.Tests;

public class VehicleShareTests
{
    [Fact]
    public void DomainAllowsOnlyInvitedUserToRespondOnce()
    {
        var invited = Guid.NewGuid();
        var share = new VehicleShare { InvitedUserId = invited };
        Assert.Equal(VehicleShareStatus.Pending, share.Status);
        Assert.Throws<UnauthorizedAccessException>(() => share.Respond(Guid.NewGuid(), true, DateTimeOffset.UtcNow));
        share.Respond(invited, true, DateTimeOffset.UtcNow);
        Assert.Equal(VehicleShareStatus.Accepted, share.Status);
        Assert.NotNull(share.RespondedAt);
        Assert.Throws<InvalidOperationException>(() => share.Respond(invited, false, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task InviteEndpointValidatesOwnerEmailAndDuplicates()
    {
        var owner = Guid.NewGuid();
        var service = new FakeShares(owner);
        var method = Method("InviteAsync");
        Assert.Equal(401, await Status(method, [7, new VehicleInviteInput("guest@example.com"),
            new ClaimsPrincipal(), service, CancellationToken.None]));
        Assert.Equal(400, await Status(method, [7, new VehicleInviteInput("bad-email"),
            Principal(owner), service, CancellationToken.None]));
        Assert.Equal(0, service.Invites);
        Assert.Equal(201, await Status(method, [7, new VehicleInviteInput("guest@example.com"),
            Principal(owner), service, CancellationToken.None]));
        Assert.Equal(owner, service.LastUser);
        Assert.Equal(409, await Status(method, [7, new VehicleInviteInput("guest@example.com"),
            Principal(owner), service, CancellationToken.None]));
        Assert.Equal(404, await Status(method, [7, new VehicleInviteInput("guest@example.com"),
            Principal(Guid.NewGuid()), service, CancellationToken.None]));
    }

    [Fact]
    public async Task ResponseAndRevokeEndpointsUseJwtIdentity()
    {
        var owner = Guid.NewGuid();
        var invited = Guid.NewGuid();
        var service = new FakeShares(owner) { Invited = invited };
        Assert.Equal(401, await Status(Method("AcceptAsync"),
            [1, new ClaimsPrincipal(), service, CancellationToken.None]));
        Assert.Equal(404, await Status(Method("AcceptAsync"),
            [1, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(200, await Status(Method("AcceptAsync"),
            [1, Principal(invited), service, CancellationToken.None]));
        Assert.Equal(404, await Status(Method("RevokeAsync"),
            [7, 1, Principal(invited), service, CancellationToken.None]));
        Assert.Equal(204, await Status(Method("RevokeAsync"),
            [7, 1, Principal(owner), service, CancellationToken.None]));
    }

    private static MethodInfo Method(string name) =>
        typeof(VehicleShareEndpoints).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
    private static async Task<int?> Status(MethodInfo method, object?[] args) =>
        ((IStatusCodeHttpResult)await (Task<IResult>)method.Invoke(null, args)!).StatusCode;
    private static ClaimsPrincipal Principal(Guid user) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, user.ToString())], "test"));

    private sealed class FakeShares(Guid owner) : IVehicleShareService
    {
        public Guid Invited { get; set; }
        public int Invites { get; private set; }
        public Guid LastUser { get; private set; }
        public Task<InviteResult> InviteAsync(Guid ownerId, int vehicleId, string email,
            CancellationToken cancellationToken)
        {
            LastUser = ownerId;
            if (ownerId != owner) return Task.FromResult(new InviteResult(InviteStatus.VehicleNotFound));
            Invites++;
            return Task.FromResult(new InviteResult(Invites == 1 ? InviteStatus.Created : InviteStatus.Duplicate,
                new VehicleShareDto(1, vehicleId, Invited, email, owner, "Pending", DateTimeOffset.UtcNow, null)));
        }
        public Task<IReadOnlyList<VehicleShareDto>> GetInvitationsAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VehicleShareDto>>([]);
        public Task<InvitationResponseResult> RespondAsync(Guid userId, int shareId, bool accept,
            CancellationToken cancellationToken) => Task.FromResult(new InvitationResponseResult(
                userId == Invited ? InvitationResponseStatus.Updated : InvitationResponseStatus.NotFound,
                new VehicleShareDto(shareId, 7, Invited, "guest@example.com", owner,
                    accept ? "Accepted" : "Rejected", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        public Task<IReadOnlyList<VehicleShareDto>?> GetVehicleSharesAsync(Guid ownerId, int vehicleId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleShareDto>?>([]);
        public Task<bool> RevokeAsync(Guid ownerId, int vehicleId, int shareId,
            CancellationToken cancellationToken) => Task.FromResult(ownerId == owner);
        public Task<IReadOnlyList<VehicleDto>> GetSharedVehiclesAsync(Guid userId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VehicleDto>>([]);
    }
}

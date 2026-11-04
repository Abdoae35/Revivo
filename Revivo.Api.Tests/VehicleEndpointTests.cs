using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Revivo.Api.Vehicles;
using Revivo.Application.Vehicles;
using Revivo.Domain.Entities;

namespace Revivo.Api.Tests;

public class VehicleEndpointTests
{
    [Fact]
    public async Task CreateUsesAuthenticatedOwnerAndRejectsInvalidInput()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingVehicles(owner);
        var method = typeof(VehicleEndpoints).GetMethod("CreateAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var valid = new VehicleInput(1, 2024, 100, null, null);
        var response = await (Task<IResult>)method.Invoke(null, [valid, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(201, ((IStatusCodeHttpResult)response).StatusCode);
        Assert.Equal(owner, service.LastOwner);

        var invalid = new VehicleInput(1, 1800, -1, null, null);
        response = await (Task<IResult>)method.Invoke(null, [invalid, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(400, ((IStatusCodeHttpResult)response).StatusCode);
        Assert.Equal(1, service.CreateCalls);
    }

    [Fact]
    public async Task OtherOwnerCannotReadVehicle()
    {
        var owner = Guid.NewGuid();
        var method = typeof(VehicleEndpoints).GetMethod("GetAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var service = new RecordingVehicles(owner);
        var response = await (Task<IResult>)method.Invoke(null, [1, Principal(Guid.NewGuid()), service, CancellationToken.None])!;
        Assert.Equal(404, ((IStatusCodeHttpResult)response).StatusCode);
        response = await (Task<IResult>)method.Invoke(null, [1, Principal(owner), service, CancellationToken.None])!;
        Assert.Equal(200, ((IStatusCodeHttpResult)response).StatusCode);
    }

    [Fact]
    public void DomainRejectsInvalidYearAndMileage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vehicle { Year = 1800 });
        var vehicle = new Vehicle { Year = 2024 };
        Assert.Throws<ArgumentOutOfRangeException>(() => vehicle.CurrentMileageKm = -1);
    }

    private static ClaimsPrincipal Principal(Guid owner) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, owner.ToString())], "test"));

    private sealed class RecordingVehicles(Guid owner) : IVehicleService
    {
        public Guid LastOwner { get; private set; }
        public int CreateCalls { get; private set; }
        public Task<VehicleWriteResult> CreateAsync(Guid ownerId, VehicleInput input, CancellationToken cancellationToken)
        {
            LastOwner = ownerId; CreateCalls++;
            return Task.FromResult(new VehicleWriteResult(VehicleWriteStatus.Success,
                new VehicleDto(1, ownerId, input.CarModelId, input.Year, input.CurrentMileageKm, input.Nickname, input.ImageUrl, DateTimeOffset.UtcNow)));
        }
        public Task<IReadOnlyList<VehicleDto>> GetAllAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VehicleDto>>([]);
        public Task<VehicleDto?> GetAsync(Guid ownerId, int id, CancellationToken cancellationToken) =>
            Task.FromResult<VehicleDto?>(ownerId == owner ? new VehicleDto(id, owner, 1, 2024, 100, null, null, DateTimeOffset.UtcNow) : null);
        public Task<VehicleWriteResult> UpdateAsync(Guid ownerId, int id, VehicleInput input, CancellationToken cancellationToken) =>
            Task.FromResult(new VehicleWriteResult(VehicleWriteStatus.NotFound));
        public Task<VehicleDeleteStatus> DeleteAsync(Guid ownerId, int id, CancellationToken cancellationToken) =>
            Task.FromResult(VehicleDeleteStatus.NotFound);
    }
}

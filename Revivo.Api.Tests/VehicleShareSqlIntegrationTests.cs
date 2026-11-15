using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Revivo.Api.Vehicles;
using Revivo.Application.Maintenance;
using Revivo.Application.Vehicles;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Identity;
using Revivo.Infrastructure.Maintenance;
using Revivo.Infrastructure.Persistence;
using Revivo.Infrastructure.Statistics;
using Revivo.Infrastructure.Vehicles;
using Revivo.Infrastructure.Warranties;

namespace Revivo.Api.Tests;

public class VehicleShareSqlIntegrationTests
{
    [Fact]
    public async Task InvitationLifecycleControlsReadAccessAndPreservesOwnerOnlyWrites()
    {
        if (Environment.GetEnvironmentVariable("REVIVO_SQL_INTEGRATION") != "1") return;
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(VehicleShareEndpoints).Assembly).Build();
        var connection = configuration.GetConnectionString("RevivoDb")
            ?? throw new InvalidOperationException("RevivoDb user secret is missing.");
        var options = new DbContextOptionsBuilder<RevivoDbContext>().UseSqlServer(connection).Options;
        await using var db = new RevivoDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = User();
        var invited = User();
        var unrelated = User();
        db.Users.AddRange(owner, invited, unrelated);
        var model = new CarModel { Name = "SharingFixture", CarMake = new CarMake
            { Name = $"SharingMake-{Guid.NewGuid():N}" } };
        var vehicle = new Vehicle { OwnerId = owner.Id, CarModel = model, Year = 2024,
            CurrentMileageKm = 1000 };
        var item = new MaintenanceItem { Name = "Oil", Category = new MaintenanceCategory
            { Name = $"SharingCategory-{Guid.NewGuid():N}" } };
        db.Vehicles.Add(vehicle);
        var record = new MaintenanceRecord { Vehicle = vehicle, Item = item,
            MaintenanceDate = new DateOnly(2024, 1, 1), MileageKm = 1000, CostEgp = 100.25m };
        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();

        var shares = new VehicleShareService(db);
        var vehicles = new VehicleService(db);
        var maintenance = new MaintenanceService(db);
        var statistics = new StatisticsService(db);
        var periodic = new PeriodicMaintenanceService(db);
        var warranties = new WarrantyService(db);
        var odometer = new OdometerService(db);
        var maintenanceInput = new MaintenanceInput("Fluids", "Oil", new DateOnly(2024, 1, 2),
            1000, 10m, null);

        Assert.Null(await vehicles.GetAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Null(await maintenance.GetPageAsync(invited.Id, vehicle.Id, 1, 10, CancellationToken.None));
        Assert.Null(await statistics.GetOverviewAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Equal(InviteStatus.VehicleNotFound, (await shares.InviteAsync(unrelated.Id, vehicle.Id,
            invited.Email!, CancellationToken.None)).Status);
        Assert.Equal(InviteStatus.UserNotFound, (await shares.InviteAsync(owner.Id, vehicle.Id,
            "missing@example.com", CancellationToken.None)).Status);
        Assert.Equal(InviteStatus.SelfInvite, (await shares.InviteAsync(owner.Id, vehicle.Id,
            owner.Email!, CancellationToken.None)).Status);

        var invitation = await shares.InviteAsync(owner.Id, vehicle.Id, invited.Email!, CancellationToken.None);
        Assert.Equal(InviteStatus.Created, invitation.Status);
        var shareId = invitation.Share!.Id;
        Assert.Equal(InviteStatus.Duplicate, (await shares.InviteAsync(owner.Id, vehicle.Id,
            invited.Email!, CancellationToken.None)).Status);
        Assert.Single(await shares.GetInvitationsAsync(invited.Id, CancellationToken.None));
        Assert.Empty(await shares.GetSharedVehiclesAsync(invited.Id, CancellationToken.None));
        Assert.Null(await vehicles.GetAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Equal(InvitationResponseStatus.NotFound, (await shares.RespondAsync(unrelated.Id,
            shareId, true, CancellationToken.None)).Status);
        Assert.Equal(InvitationResponseStatus.Updated, (await shares.RespondAsync(invited.Id,
            shareId, true, CancellationToken.None)).Status);
        Assert.Equal(InviteStatus.Duplicate, (await shares.InviteAsync(owner.Id, vehicle.Id,
            invited.Email!, CancellationToken.None)).Status);
        Assert.Equal(InvitationResponseStatus.NotPending, (await shares.RespondAsync(invited.Id,
            shareId, false, CancellationToken.None)).Status);
        Assert.Empty(await shares.GetInvitationsAsync(invited.Id, CancellationToken.None));

        Assert.NotNull(await vehicles.GetAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Single(await shares.GetSharedVehiclesAsync(invited.Id, CancellationToken.None));
        Assert.Single((await maintenance.GetPageAsync(invited.Id, vehicle.Id, 1, 10,
            CancellationToken.None))!.Items);
        Assert.NotNull(await maintenance.GetAsync(invited.Id, vehicle.Id, record.Id, CancellationToken.None));
        Assert.NotNull(await statistics.GetOverviewAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.NotNull(await statistics.GetMileageAsync(invited.Id, vehicle.Id, 2024, CancellationToken.None));
        Assert.NotNull(await statistics.GetExpensesAsync(invited.Id, vehicle.Id, 2024, CancellationToken.None));
        Assert.NotNull(await periodic.GetStatusesAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.NotNull(await warranties.GetAllAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.NotNull(await odometer.GetHistoryAsync(invited.Id, vehicle.Id, 1, 10, CancellationToken.None));
        Assert.Equal(VehicleWriteStatus.NotFound, (await vehicles.UpdateAsync(invited.Id, vehicle.Id,
            new VehicleInput(model.Id, 2024, 1000, null, null), CancellationToken.None)).Status);
        Assert.Equal(VehicleDeleteStatus.NotFound, await vehicles.DeleteAsync(invited.Id, vehicle.Id,
            CancellationToken.None));
        Assert.Equal(MaintenanceWriteStatus.VehicleNotFound, (await maintenance.CreateAsync(invited.Id,
            vehicle.Id, maintenanceInput, CancellationToken.None)).Status);
        Assert.Equal(MaintenanceWriteStatus.VehicleNotFound, (await maintenance.UpdateAsync(invited.Id,
            vehicle.Id, record.Id, maintenanceInput, CancellationToken.None)).Status);
        Assert.Equal(MaintenanceWriteStatus.VehicleNotFound, await maintenance.DeleteAsync(invited.Id,
            vehicle.Id, record.Id, CancellationToken.None));
        Assert.Null(await shares.GetVehicleSharesAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Single((await shares.GetVehicleSharesAsync(owner.Id, vehicle.Id, CancellationToken.None))!);
        Assert.False(await shares.RevokeAsync(invited.Id, vehicle.Id, shareId, CancellationToken.None));
        Assert.True(await shares.RevokeAsync(owner.Id, vehicle.Id, shareId, CancellationToken.None));
        Assert.Null(await vehicles.GetAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Null(await maintenance.GetAsync(invited.Id, vehicle.Id, record.Id, CancellationToken.None));
        Assert.Null(await statistics.GetOverviewAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Empty(await shares.GetSharedVehiclesAsync(invited.Id, CancellationToken.None));
        Assert.Null(await vehicles.GetAsync(unrelated.Id, vehicle.Id, CancellationToken.None));

        var rejected = await shares.InviteAsync(owner.Id, vehicle.Id, invited.Email!, CancellationToken.None);
        Assert.Equal(InviteStatus.Created, rejected.Status);
        Assert.Equal(InvitationResponseStatus.Updated, (await shares.RespondAsync(invited.Id,
            rejected.Share!.Id, false, CancellationToken.None)).Status);
        Assert.Empty(await shares.GetInvitationsAsync(invited.Id, CancellationToken.None));
        Assert.Null(await vehicles.GetAsync(invited.Id, vehicle.Id, CancellationToken.None));
        Assert.Equal(InviteStatus.Created, (await shares.InviteAsync(owner.Id, vehicle.Id,
            invited.Email!, CancellationToken.None)).Status);
        await transaction.RollbackAsync();
    }

    private static ApplicationUser User()
    {
        var id = Guid.NewGuid();
        var email = $"sharing-{id:N}@example.com";
        return new ApplicationUser { Id = id, Email = email, NormalizedEmail = email.ToUpperInvariant(),
            UserName = email, NormalizedUserName = email.ToUpperInvariant() };
    }
}

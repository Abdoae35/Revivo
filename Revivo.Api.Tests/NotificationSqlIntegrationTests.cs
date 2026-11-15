using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Revivo.Api.Notifications;
using Revivo.Application.Notifications;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Maintenance;
using Revivo.Infrastructure.Persistence;
using Revivo.Infrastructure.Notifications;
using Revivo.Infrastructure.Warranties;

namespace Revivo.Api.Tests;

public class NotificationSqlIntegrationTests
{
    [Fact]
    public async Task SettingsPersistAndAlertsUseOwnedVehicle()
    {
        if (Environment.GetEnvironmentVariable("REVIVO_SQL_INTEGRATION") != "1")
        {
            throw Xunit.Sdk.SkipException.ForSkip("Set REVIVO_SQL_INTEGRATION=1 when SQL Server is available.");
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(NotificationEndpoints).Assembly).Build();
        var connectionString = configuration.GetConnectionString("RevivoDb")
            ?? throw new InvalidOperationException("RevivoDb user secret is missing.");
        var options = new DbContextOptionsBuilder<RevivoDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new RevivoDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var owner = Guid.NewGuid();
        var make = new CarMake { Name = $"NotificationTest-{Guid.NewGuid():N}" };
        var model = new CarModel { Name = "Temporary", CarMake = make };
        var vehicle = new Vehicle { OwnerId = owner, CarModel = model, Year = 2024, CurrentMileageKm = 1000 };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var service = new NotificationService(db, new PeriodicMaintenanceService(db), new WarrantyService(db));
        var defaults = await service.GetSettingsAsync(owner, vehicle.Id, CancellationToken.None);
        Assert.NotNull(defaults);
        Assert.Equal(0, defaults.Id);
        Assert.False(defaults.WeeklyReminderEnabled);

        var input = new NotificationSettingsInput(true, 5, new TimeOnly(8, 30), "UTC", true, false);
        var updated = await service.UpdateSettingsAsync(owner, vehicle.Id, input, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.True(updated.Id > 0);
        Assert.Equal(5, updated.ReminderDayOfWeek);
        Assert.Equal("UTC", updated.TimeZoneId);
        var stored = await db.NotificationSettings.AsNoTracking().SingleAsync(item => item.VehicleId == vehicle.Id);
        Assert.Equal(updated.Id, stored.Id);
        Assert.Equal(new TimeOnly(8, 30), stored.ReminderTime);
        Assert.Null(await service.GetSettingsAsync(Guid.NewGuid(), vehicle.Id, CancellationToken.None));

        var alerts = await service.GetAlertsAsync(owner, vehicle.Id, CancellationToken.None);
        Assert.NotNull(alerts);
        Assert.Empty(alerts.Maintenance);
        Assert.Empty(alerts.Warranties);
        Assert.Null(await service.GetAlertsAsync(Guid.NewGuid(), vehicle.Id, CancellationToken.None));
        await transaction.RollbackAsync();
    }
}

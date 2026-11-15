using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Revivo.Api.Statistics;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;
using Revivo.Infrastructure.Statistics;

namespace Revivo.Api.Tests;

public class StatisticsSqlIntegrationTests
{
    [Fact]
    public async Task QueriesUseOwnedVehicleAndExistingRecordsWithoutWritingHistory()
    {
        if (Environment.GetEnvironmentVariable("REVIVO_SQL_INTEGRATION") != "1") return;

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(StatisticsEndpoints).Assembly).Build();
        var connectionString = configuration.GetConnectionString("RevivoDb")
            ?? throw new InvalidOperationException("RevivoDb user secret is missing.");
        var options = new DbContextOptionsBuilder<RevivoDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new RevivoDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var owner = Guid.NewGuid();
        var make = new CarMake { Name = $"StatisticsTest-{Guid.NewGuid():N}" };
        var model = new CarModel { Name = "Temporary", CarMake = make };
        var vehicle = new Vehicle { OwnerId = owner, CarModel = model, Year = 2024, CurrentMileageKm = 4000 };
        var category = new MaintenanceCategory { Name = $"StatisticFluid-{Guid.NewGuid():N}" };
        var item = new MaintenanceItem { Name = "Oil", Category = category };
        db.Vehicles.Add(vehicle);
        db.MaintenanceItems.Add(item);
        await db.SaveChangesAsync();

        db.OdometerReadings.AddRange(
            new OdometerReading { VehicleId = vehicle.Id, MileageKm = 1000,
                RecordedAt = At(2024, 1, 1), RecordedByUserId = owner },
            new OdometerReading { VehicleId = vehicle.Id, MileageKm = 2000,
                RecordedAt = At(2024, 2, 1), RecordedByUserId = owner },
            new OdometerReading { VehicleId = vehicle.Id, MileageKm = 3000,
                RecordedAt = At(2024, 3, 1), RecordedByUserId = owner },
            new OdometerReading { VehicleId = vehicle.Id, MileageKm = 4000,
                RecordedAt = At(2025, 1, 1), RecordedByUserId = owner });
        db.MaintenanceRecords.AddRange(
            new MaintenanceRecord { VehicleId = vehicle.Id, Item = item,
                MaintenanceDate = new DateOnly(2024, 1, 10), MileageKm = 1500, CostEgp = 100.25m },
            new MaintenanceRecord { VehicleId = vehicle.Id, Item = item,
                MaintenanceDate = new DateOnly(2024, 2, 10), MileageKm = 2500, CostEgp = 200.10m });
        await db.SaveChangesAsync();

        var service = new StatisticsService(db);
        var mileage = await service.GetMileageAsync(owner, vehicle.Id, 2024, CancellationToken.None);
        Assert.NotNull(mileage);
        Assert.Equal(3000m, mileage.TotalYearDistanceKm);
        Assert.Equal(1000m, mileage.MonthlyDistanceKm[0].DistanceKm);
        Assert.Equal("Estimated", mileage.MonthlyDistanceKm[2].Status);
        var expenses = await service.GetExpensesAsync(owner, vehicle.Id, 2024, CancellationToken.None);
        Assert.NotNull(expenses);
        Assert.Equal(300.35m, expenses.TotalExpenses);
        Assert.Equal(2, expenses.MaintenanceOperationsCount);
        Assert.Equal(300.35m, Assert.Single(expenses.ExpensesByCategory).AmountEgp);
        var overview = await service.GetOverviewAsync(owner, vehicle.Id, CancellationToken.None);
        Assert.NotNull(overview);
        Assert.Equal(4000, overview.CurrentMileageKm);
        Assert.Equal(300.35m, overview.LifetimeMaintenanceCost);
        Assert.Null(await service.GetOverviewAsync(Guid.NewGuid(), vehicle.Id, CancellationToken.None));
        Assert.Null(await service.GetMileageAsync(Guid.NewGuid(), vehicle.Id, 2024, CancellationToken.None));
        Assert.Null(await service.GetExpensesAsync(owner, int.MaxValue, 2024, CancellationToken.None));
        await transaction.RollbackAsync();
    }

    private static DateTimeOffset At(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);
}

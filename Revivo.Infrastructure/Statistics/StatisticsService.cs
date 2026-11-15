using Microsoft.EntityFrameworkCore;
using Revivo.Application.Statistics;
using Revivo.Domain.Statistics;
using Revivo.Infrastructure.Persistence;
using Revivo.Infrastructure.Vehicles;

namespace Revivo.Infrastructure.Statistics;

public sealed class StatisticsService(RevivoDbContext db) : IStatisticsService
{
    public async Task<StatisticsOverviewDto?> GetOverviewAsync(Guid userId, int vehicleId,
        CancellationToken cancellationToken)
    {
        var currentMileage = await OwnedMileageAsync(userId, vehicleId, cancellationToken);
        if (currentMileage is null) return null;
        var now = DateTimeOffset.UtcNow;
        var readings = await ReadingsAsync(vehicleId, cancellationToken);
        var records = await ExpensesAsync(vehicleId, cancellationToken);
        var month = MileageAnalyticsCalculator.CalculateMonth(readings, now.Year, now.Month, now);
        var year = MileageAnalyticsCalculator.CalculateYear(readings, now.Year, now).Total;
        var expenses = ExpenseAnalyticsCalculator.CalculateOverview(records, DateOnly.FromDateTime(now.UtcDateTime));
        return new(vehicleId, currentMileage.Value, month.DistanceKm, month.Confidence.ToString(), month.IsPartial,
            year.DistanceKm, year.Confidence.ToString(), year.IsPartial,
            MileageAnalyticsCalculator.AverageCompleteMonth(readings, now), expenses.Lifetime,
            expenses.Month, expenses.Year, expenses.Count, "EGP");
    }

    public async Task<MileageStatisticsDto?> GetMileageAsync(Guid userId, int vehicleId, int year,
        CancellationToken cancellationToken)
    {
        var currentMileage = await OwnedMileageAsync(userId, vehicleId, cancellationToken);
        if (currentMileage is null) return null;
        var now = DateTimeOffset.UtcNow;
        var readings = await ReadingsAsync(vehicleId, cancellationToken);
        var analysis = MileageAnalyticsCalculator.CalculateYear(readings, year, now);
        return new(vehicleId, year, currentMileage.Value, analysis.Total.DistanceKm,
            analysis.Total.Confidence.ToString(), analysis.Total.IsPartial,
            MileageAnalyticsCalculator.AverageCompleteMonth(readings, now),
            analysis.Months.Select(month => new MonthlyMileageDto(month.Month, month.Distance.DistanceKm,
                month.Distance.Confidence.ToString(), month.Distance.IsPartial)).ToArray());
    }

    public async Task<ExpenseStatisticsDto?> GetExpensesAsync(Guid userId, int vehicleId, int year,
        CancellationToken cancellationToken)
    {
        if (await OwnedMileageAsync(userId, vehicleId, cancellationToken) is null) return null;
        return ExpenseAnalyticsCalculator.CalculateYear(vehicleId, year,
            await ExpensesAsync(vehicleId, cancellationToken));
    }

    private async Task<int?> OwnedMileageAsync(Guid userId, int vehicleId, CancellationToken cancellationToken) =>
        !await VehicleAccess.CanReadAsync(db, userId, vehicleId, cancellationToken) ? null :
        await db.Vehicles.AsNoTracking().Where(vehicle => vehicle.Id == vehicleId)
            .Select(vehicle => (int?)vehicle.CurrentMileageKm).SingleOrDefaultAsync(cancellationToken);

    private Task<List<OdometerPoint>> ReadingsAsync(int vehicleId, CancellationToken cancellationToken) =>
        db.OdometerReadings.AsNoTracking().Where(reading => reading.VehicleId == vehicleId)
            .OrderBy(reading => reading.RecordedAt).ThenBy(reading => reading.Id)
            .Select(reading => new OdometerPoint(reading.RecordedAt, reading.MileageKm))
            .ToListAsync(cancellationToken);

    private Task<List<ExpenseRecord>> ExpensesAsync(int vehicleId, CancellationToken cancellationToken) =>
        db.MaintenanceRecords.AsNoTracking().Where(record => record.VehicleId == vehicleId)
            .Select(record => new ExpenseRecord(record.Id, record.MaintenanceDate, record.CostEgp,
                record.Item.Category.Name, record.Item.Name)).ToListAsync(cancellationToken);
}

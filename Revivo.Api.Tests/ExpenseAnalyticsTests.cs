using Revivo.Application.Statistics;

namespace Revivo.Api.Tests;

public class ExpenseAnalyticsTests
{
    [Fact]
    public void MonthlyYearlyCategoryAndTopCostsUseMaintenanceDateAndDecimalMoney()
    {
        var rows = new[]
        {
            new ExpenseRecord(1, new DateOnly(2026, 1, 10), 100.25m, "Fluids", "Oil"),
            new ExpenseRecord(2, new DateOnly(2026, 1, 20), 200.10m, "Fluids", "Coolant"),
            new ExpenseRecord(3, new DateOnly(2026, 2, 1), 1000.99m, "Electrical", "Battery"),
            new ExpenseRecord(4, new DateOnly(2025, 12, 31), 50.05m, "Fluids", "Old Oil")
        };
        var year = ExpenseAnalyticsCalculator.CalculateYear(7, 2026, rows);
        Assert.Equal("EGP", year.Currency);
        Assert.Equal(1301.34m, year.TotalExpenses);
        Assert.Equal(3, year.MaintenanceOperationsCount);
        Assert.Equal(12, year.MonthlyExpenses.Count);
        Assert.Equal(300.35m, year.MonthlyExpenses[0].AmountEgp);
        Assert.Equal(2, year.MonthlyExpenses[0].OperationsCount);
        Assert.Equal(1000.99m, year.MonthlyExpenses[1].AmountEgp);
        Assert.Equal(0m, year.MonthlyExpenses[2].AmountEgp);
        Assert.Equal("Electrical", year.ExpensesByCategory[0].Category);
        Assert.Equal(300.35m, year.ExpensesByCategory[1].AmountEgp);
        Assert.Equal([3, 2, 1], year.TopExpensiveMaintenanceRecords.Select(record => record.Id));

        var overview = ExpenseAnalyticsCalculator.CalculateOverview(rows, new DateOnly(2026, 1, 31));
        Assert.Equal(1351.39m, overview.Lifetime);
        Assert.Equal(300.35m, overview.Month);
        Assert.Equal(1301.34m, overview.Year);
        Assert.Equal(4, overview.Count);
    }

    [Fact]
    public void EmptyHistoryProducesTwelveZeroMonthsAndNoDuplicates()
    {
        var result = ExpenseAnalyticsCalculator.CalculateYear(7, 2026, []);
        Assert.Equal(0m, result.TotalExpenses);
        Assert.Equal(0, result.MaintenanceOperationsCount);
        Assert.All(result.MonthlyExpenses, month => Assert.Equal(0m, month.AmountEgp));
        Assert.Empty(result.ExpensesByCategory);
        Assert.Empty(result.TopExpensiveMaintenanceRecords);
        Assert.Equal((0m, 0m, 0m, 0),
            ExpenseAnalyticsCalculator.CalculateOverview([], new DateOnly(2026, 1, 1)));
    }
}

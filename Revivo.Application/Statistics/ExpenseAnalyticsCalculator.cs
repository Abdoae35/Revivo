namespace Revivo.Application.Statistics;

public static class ExpenseAnalyticsCalculator
{
    public static ExpenseStatisticsDto CalculateYear(int vehicleId, int year,
        IEnumerable<ExpenseRecord> records)
    {
        var selected = records.Where(record => record.MaintenanceDate.Year == year).ToArray();
        var monthly = Enumerable.Range(1, 12).Select(month =>
        {
            var entries = selected.Where(record => record.MaintenanceDate.Month == month).ToArray();
            return new MonthlyExpenseDto(month, entries.Sum(record => record.CostEgp), entries.Length);
        }).ToArray();
        var categories = selected.GroupBy(record => record.Category)
            .Select(group => new CategoryExpenseDto(group.Key, group.Sum(record => record.CostEgp), group.Count()))
            .OrderByDescending(category => category.AmountEgp).ThenBy(category => category.Category)
            .ToArray();
        var top = selected.OrderByDescending(record => record.CostEgp)
            .ThenByDescending(record => record.MaintenanceDate).ThenByDescending(record => record.Id)
            .Take(5).Select(record => new ExpensiveMaintenanceDto(record.Id, record.MaintenanceDate,
                record.Category, record.ItemName, record.CostEgp)).ToArray();
        return new(vehicleId, year, selected.Sum(record => record.CostEgp), selected.Length, "EGP",
            monthly, categories, top);
    }

    public static (decimal Lifetime, decimal Month, decimal Year, int Count) CalculateOverview(
        IEnumerable<ExpenseRecord> records, DateOnly today)
    {
        var items = records.ToArray();
        return (items.Sum(record => record.CostEgp),
            items.Where(record => record.MaintenanceDate.Year == today.Year &&
                record.MaintenanceDate.Month == today.Month).Sum(record => record.CostEgp),
            items.Where(record => record.MaintenanceDate.Year == today.Year).Sum(record => record.CostEgp),
            items.Length);
    }
}

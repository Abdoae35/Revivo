namespace Revivo.Application.Statistics;

public sealed record StatisticsOverviewDto(int VehicleId, int CurrentMileageKm,
    decimal? ThisMonthDistanceKm, string ThisMonthDistanceStatus, bool ThisMonthIsPartial,
    decimal? ThisYearDistanceKm, string ThisYearDistanceStatus, bool ThisYearIsPartial,
    decimal? AverageMonthlyDistanceKm, decimal LifetimeMaintenanceCost,
    decimal ThisMonthMaintenanceCost, decimal ThisYearMaintenanceCost,
    int MaintenanceOperationsCount, string Currency);

public sealed record MonthlyMileageDto(int Month, decimal? DistanceKm, string Status, bool IsPartial);
public sealed record MileageStatisticsDto(int VehicleId, int Year, int CurrentMileageKm,
    decimal? TotalYearDistanceKm, string TotalYearDistanceStatus, bool IsPartial,
    decimal? AverageMonthlyDistanceKm, IReadOnlyList<MonthlyMileageDto> MonthlyDistanceKm);

public sealed record ExpenseRecord(int Id, DateOnly MaintenanceDate, decimal CostEgp,
    string Category, string ItemName);
public sealed record MonthlyExpenseDto(int Month, decimal AmountEgp, int OperationsCount);
public sealed record CategoryExpenseDto(string Category, decimal AmountEgp, int OperationsCount);
public sealed record ExpensiveMaintenanceDto(int Id, DateOnly MaintenanceDate, string Category,
    string ItemName, decimal CostEgp);
public sealed record ExpenseStatisticsDto(int VehicleId, int Year, decimal TotalExpenses,
    int MaintenanceOperationsCount, string Currency, IReadOnlyList<MonthlyExpenseDto> MonthlyExpenses,
    IReadOnlyList<CategoryExpenseDto> ExpensesByCategory,
    IReadOnlyList<ExpensiveMaintenanceDto> TopExpensiveMaintenanceRecords);

public interface IStatisticsService
{
    Task<StatisticsOverviewDto?> GetOverviewAsync(Guid userId, int vehicleId, CancellationToken cancellationToken);
    Task<MileageStatisticsDto?> GetMileageAsync(Guid userId, int vehicleId, int year,
        CancellationToken cancellationToken);
    Task<ExpenseStatisticsDto?> GetExpensesAsync(Guid userId, int vehicleId, int year,
        CancellationToken cancellationToken);
}

using Revivo.Domain.Entities;

namespace Revivo.Domain.Maintenance;

public static class MaintenanceHistorySelector
{
    public static MaintenanceRecord? Latest(IEnumerable<MaintenanceRecord> records) =>
        records.OrderByDescending(record => record.MaintenanceDate)
            .ThenByDescending(record => record.CreatedAt)
            .ThenByDescending(record => record.Id).FirstOrDefault();
}

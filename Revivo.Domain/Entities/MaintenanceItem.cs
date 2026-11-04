namespace Revivo.Domain.Entities;

public sealed class MaintenanceItem
{
    public int Id { get; set; }
    public int MaintenanceCategoryId { get; set; }
    public required string Name { get; set; }
    public MaintenanceCategory Category { get; set; } = null!;
    public ICollection<MaintenanceRecord> Records { get; set; } = new List<MaintenanceRecord>();
    public ICollection<MaintenanceSchedule> Schedules { get; set; } = new List<MaintenanceSchedule>();
}

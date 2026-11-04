namespace Revivo.Domain.Entities;

public sealed class MaintenanceCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<MaintenanceItem> Items { get; set; } = new List<MaintenanceItem>();
}

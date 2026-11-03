namespace Revivo.Domain.Entities;

public sealed class CarModel
{
    public int Id { get; set; }
    public int CarMakeId { get; set; }
    public required string Name { get; set; }
    public int? YearStart { get; set; }
    public int? YearEnd { get; set; }
    public CarMake CarMake { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

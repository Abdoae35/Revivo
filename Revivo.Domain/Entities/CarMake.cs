namespace Revivo.Domain.Entities;

public sealed class CarMake
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<CarModel> Models { get; set; } = new List<CarModel>();
}

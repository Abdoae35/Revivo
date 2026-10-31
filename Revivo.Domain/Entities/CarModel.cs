namespace Revivo.Domain.Entities;

public sealed class CarModel
{
    public int Id { get; set; }
    public int CarMakeId { get; set; }
    public required string Name { get; set; }
}

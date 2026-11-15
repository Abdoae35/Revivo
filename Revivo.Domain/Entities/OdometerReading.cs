namespace Revivo.Domain.Entities;

public sealed class OdometerReading
{
    private int _mileageKm;

    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public int MileageKm
    {
        get => _mileageKm;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Mileage cannot be negative.");
            _mileageKm = value;
        }
    }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid RecordedByUserId { get; set; }
}

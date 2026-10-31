namespace Revivo.Domain.Entities;

public sealed class Vehicle
{
    private int _year;
    private int _currentMileageKm;

    public int Id { get; set; }
    public Guid OwnerId { get; set; }
    public int CarModelId { get; set; }

    public required int Year
    {
        get => _year;
        set
        {
            if (value < 1886 || value > DateTime.UtcNow.Year + 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Vehicle year must be between 1886 and next year.");
            _year = value;
        }
    }

    public int CurrentMileageKm
    {
        get => _currentMileageKm;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Mileage cannot be negative.");
            _currentMileageKm = value;
        }
    }

    public string? Nickname { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

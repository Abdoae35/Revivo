namespace Revivo.Domain.Entities;

public sealed class MaintenanceSchedule
{
    private int? _intervalKm;
    private int? _intervalMonths;
    private int _warningKm = 1000;
    private int _warningDays = 30;

    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public int MaintenanceItemId { get; set; }
    public MaintenanceItem Item { get; set; } = null!;

    public int? IntervalKm
    {
        get => _intervalKm;
        set
        {
            if (value is <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Mileage interval must be positive.");
            _intervalKm = value;
        }
    }

    public int? IntervalMonths
    {
        get => _intervalMonths;
        set
        {
            if (value is <= 0 or > 1200)
                throw new ArgumentOutOfRangeException(nameof(value), "Time interval must be between 1 and 1200 months.");
            _intervalMonths = value;
        }
    }

    public int WarningKm
    {
        get => _warningKm;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Mileage warning threshold cannot be negative.");
            _warningKm = value;
        }
    }

    public int WarningDays
    {
        get => _warningDays;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Day warning threshold cannot be negative.");
            _warningDays = value;
        }
    }
}

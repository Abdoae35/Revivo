namespace Revivo.Domain.Entities;

public sealed class MaintenanceRecord
{
    private DateOnly _maintenanceDate;
    private int _mileageKm;
    private decimal _costEgp;

    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public int MaintenanceItemId { get; set; }
    public MaintenanceItem Item { get; set; } = null!;
    public MaintenanceWarranty? Warranty { get; set; }

    public required DateOnly MaintenanceDate
    {
        get => _maintenanceDate;
        set
        {
            if (value < new DateOnly(1886, 1, 1) || value > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ArgumentOutOfRangeException(nameof(value), "Maintenance date must be valid and cannot be in the future.");
            _maintenanceDate = value;
        }
    }

    public int MileageKm
    {
        get => _mileageKm;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Mileage cannot be negative.");
            _mileageKm = value;
        }
    }

    public decimal CostEgp
    {
        get => _costEgp;
        set
        {
            if (value < 0 || value > 9999999999999999.99m || decimal.Round(value, 2) != value)
                throw new ArgumentOutOfRangeException(nameof(value), "Cost must be nonnegative EGP with at most two decimal places.");
            _costEgp = value;
        }
    }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

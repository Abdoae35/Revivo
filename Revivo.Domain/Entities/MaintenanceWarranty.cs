namespace Revivo.Domain.Entities;

public sealed class MaintenanceWarranty
{
    private int _durationMonths;
    private DateOnly _startDate;
    private int _warningDays = 30;

    public int MaintenanceRecordId { get; set; }
    public MaintenanceRecord Record { get; set; } = null!;

    public required int DurationMonths
    {
        get => _durationMonths;
        set
        {
            if (value is < 1 or > 1200)
                throw new ArgumentOutOfRangeException(nameof(value), "Warranty duration must be between 1 and 1200 months.");
            _durationMonths = value;
        }
    }

    public required DateOnly StartDate
    {
        get => _startDate;
        set
        {
            if (value < new DateOnly(1886, 1, 1) || value > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ArgumentOutOfRangeException(nameof(value), "Warranty start date must be valid and cannot be in the future.");
            _startDate = value;
        }
    }

    public DateOnly ExpirationDate => StartDate.AddMonths(DurationMonths);

    public int WarningDays
    {
        get => _warningDays;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Warning days cannot be negative.");
            _warningDays = value;
        }
    }

    public string? Provider { get; set; }
    public string? Brand { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

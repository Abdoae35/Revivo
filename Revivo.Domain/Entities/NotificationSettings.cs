namespace Revivo.Domain.Entities;

public sealed class NotificationSettings
{
    private int _reminderDayOfWeek = (int)DayOfWeek.Monday;
    private string _timeZoneId = "UTC";

    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid UserId { get; set; }
    public bool WeeklyReminderEnabled { get; set; }
    public int ReminderDayOfWeek
    {
        get => _reminderDayOfWeek;
        set
        {
            if (value is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(value), "Day must be between Sunday (0) and Saturday (6).");
            _reminderDayOfWeek = value;
        }
    }
    public TimeOnly ReminderTime { get; set; } = new(9, 0);
    public string TimeZoneId
    {
        get => _timeZoneId;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 100 || !IsValidTimeZone(value))
                throw new ArgumentException("A valid time zone is required.", nameof(value));
            _timeZoneId = value;
        }
    }
    public bool MaintenanceAlertsEnabled { get; set; } = true;
    public bool WarrantyAlertsEnabled { get; set; } = true;

    public static bool IsValidTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100) return false;
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }
}

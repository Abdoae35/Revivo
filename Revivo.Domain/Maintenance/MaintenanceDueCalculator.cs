using Revivo.Domain.Entities;

namespace Revivo.Domain.Maintenance;

public enum MaintenanceDueStatus { OnTrack, DueSoon, Overdue }

public sealed record MaintenanceCompletion(DateOnly Date, int MileageKm);

public sealed record MaintenanceDue(
    DateOnly? NextDueDate, long? NextDueMileageKm, int? RemainingDays, long? RemainingKm,
    MaintenanceDueStatus Status);

public static class MaintenanceDueCalculator
{
    public static MaintenanceDue Calculate(MaintenanceSchedule? schedule, MaintenanceCompletion? last,
        int currentMileageKm, DateOnly today)
    {
        if (currentMileageKm < 0) throw new ArgumentOutOfRangeException(nameof(currentMileageKm));
        if (schedule is null || last is null)
            return new(null, null, null, null, MaintenanceDueStatus.OnTrack);

        var nextDate = schedule.IntervalMonths is int months ? last.Date.AddMonths(months) : (DateOnly?)null;
        var nextMileage = schedule.IntervalKm is int km ? (long)last.MileageKm + km : (long?)null;
        var remainingDays = nextDate is DateOnly date ? date.DayNumber - today.DayNumber : (int?)null;
        var remainingKm = nextMileage is long mileage ? mileage - currentMileageKm : (long?)null;

        var overdue = remainingDays is < 0 || remainingKm is <= 0;
        var dueSoon = (remainingDays is int days && days <= schedule.WarningDays) ||
                      (remainingKm is long distance && distance <= schedule.WarningKm);
        var status = overdue ? MaintenanceDueStatus.Overdue
            : dueSoon ? MaintenanceDueStatus.DueSoon : MaintenanceDueStatus.OnTrack;
        return new(nextDate, nextMileage, remainingDays, remainingKm, status);
    }
}

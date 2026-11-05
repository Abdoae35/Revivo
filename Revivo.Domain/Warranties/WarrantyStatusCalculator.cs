using Revivo.Domain.Entities;

namespace Revivo.Domain.Warranties;

public enum WarrantyStatus { Active, ExpiringSoon, Expired }

public sealed record WarrantyAssessment(DateOnly ExpirationDate, int RemainingDays, WarrantyStatus Status);

public static class WarrantyStatusCalculator
{
    public static WarrantyAssessment Calculate(MaintenanceWarranty warranty, DateOnly today)
    {
        var expiration = warranty.ExpirationDate;
        var remaining = expiration.DayNumber - today.DayNumber;
        var status = remaining < 0 ? WarrantyStatus.Expired
            : remaining <= warranty.WarningDays ? WarrantyStatus.ExpiringSoon : WarrantyStatus.Active;
        return new(expiration, remaining, status);
    }
}

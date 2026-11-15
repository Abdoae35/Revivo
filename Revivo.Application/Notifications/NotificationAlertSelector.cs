using Revivo.Application.Maintenance;
using Revivo.Application.Warranties;
using Revivo.Domain.Maintenance;
using Revivo.Domain.Warranties;

namespace Revivo.Application.Notifications;

public static class NotificationAlertSelector
{
    public static VehicleAlertsDto Select(int vehicleId, bool maintenanceEnabled, bool warrantyEnabled,
        IReadOnlyList<MaintenanceStatusDto> maintenance, IReadOnlyList<WarrantyDto> warranties) =>
        new(vehicleId,
            maintenanceEnabled ? maintenance.Where(item => item.Status is nameof(MaintenanceDueStatus.DueSoon)
                or nameof(MaintenanceDueStatus.Overdue)).ToArray() : [],
            warrantyEnabled ? warranties.Where(item => item.Status is nameof(WarrantyStatus.ExpiringSoon)
                or nameof(WarrantyStatus.Expired)).ToArray() : []);
}

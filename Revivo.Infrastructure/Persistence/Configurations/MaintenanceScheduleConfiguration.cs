using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
    {
        builder.ToTable("MaintenanceSchedules", table =>
        {
            table.HasCheckConstraint("CK_MaintenanceSchedules_IntervalKm", "[IntervalKm] IS NULL OR [IntervalKm] > 0");
            table.HasCheckConstraint("CK_MaintenanceSchedules_IntervalMonths",
                "[IntervalMonths] IS NULL OR [IntervalMonths] BETWEEN 1 AND 1200");
            table.HasCheckConstraint("CK_MaintenanceSchedules_WarningKm", "[WarningKm] >= 0");
            table.HasCheckConstraint("CK_MaintenanceSchedules_WarningDays", "[WarningDays] >= 0");
        });
        builder.HasIndex(schedule => new { schedule.VehicleId, schedule.MaintenanceItemId }).IsUnique();
        builder.HasIndex(schedule => schedule.MaintenanceItemId);
        builder.HasOne(schedule => schedule.Vehicle).WithMany(vehicle => vehicle.MaintenanceSchedules)
            .HasForeignKey(schedule => schedule.VehicleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(schedule => schedule.Item).WithMany(item => item.Schedules)
            .HasForeignKey(schedule => schedule.MaintenanceItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

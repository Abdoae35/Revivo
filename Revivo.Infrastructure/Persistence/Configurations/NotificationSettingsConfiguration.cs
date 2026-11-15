using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint("CK_NotificationSettings_ReminderDayOfWeek",
            "[ReminderDayOfWeek] BETWEEN 0 AND 6"));
        builder.Property(settings => settings.TimeZoneId).HasMaxLength(100);
        builder.HasIndex(settings => new { settings.UserId, settings.VehicleId }).IsUnique();
        builder.HasOne(settings => settings.Vehicle).WithMany(vehicle => vehicle.NotificationSettings)
            .HasForeignKey(settings => settings.VehicleId).OnDelete(DeleteBehavior.Cascade);
    }
}

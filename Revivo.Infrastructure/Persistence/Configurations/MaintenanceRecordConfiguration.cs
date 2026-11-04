using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords", table =>
        {
            table.HasCheckConstraint("CK_MaintenanceRecords_MileageKm", "[MileageKm] >= 0");
            table.HasCheckConstraint("CK_MaintenanceRecords_CostEgp", "[CostEgp] >= 0");
            table.HasCheckConstraint("CK_MaintenanceRecords_MaintenanceDate",
                "[MaintenanceDate] >= '1886-01-01' AND [MaintenanceDate] <= CONVERT(date, GETUTCDATE())");
        });
        builder.Property(record => record.MaintenanceDate).HasColumnType("date");
        builder.Property(record => record.CostEgp).HasPrecision(18, 2);
        builder.Property(record => record.Notes).HasMaxLength(2000);
        builder.HasIndex(record => new { record.VehicleId, record.MaintenanceDate, record.CreatedAt, record.Id })
            .IsDescending(false, true, true, true);
        builder.HasIndex(record => record.MaintenanceItemId);
        builder.HasOne(record => record.Vehicle).WithMany(vehicle => vehicle.MaintenanceRecords)
            .HasForeignKey(record => record.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(record => record.Item).WithMany(item => item.Records)
            .HasForeignKey(record => record.MaintenanceItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

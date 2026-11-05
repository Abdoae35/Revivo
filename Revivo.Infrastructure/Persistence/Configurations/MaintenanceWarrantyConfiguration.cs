using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceWarrantyConfiguration : IEntityTypeConfiguration<MaintenanceWarranty>
{
    public void Configure(EntityTypeBuilder<MaintenanceWarranty> builder)
    {
        builder.ToTable("MaintenanceWarranties", table =>
        {
            table.HasCheckConstraint("CK_MaintenanceWarranties_DurationMonths", "[DurationMonths] BETWEEN 1 AND 1200");
            table.HasCheckConstraint("CK_MaintenanceWarranties_StartDate",
                "[StartDate] >= '1886-01-01' AND [StartDate] <= CONVERT(date, GETUTCDATE())");
            table.HasCheckConstraint("CK_MaintenanceWarranties_WarningDays", "[WarningDays] >= 0");
        });
        builder.HasKey(warranty => warranty.MaintenanceRecordId);
        builder.Property(warranty => warranty.StartDate).HasColumnType("date");
        builder.Property(warranty => warranty.Provider).HasMaxLength(150);
        builder.Property(warranty => warranty.Brand).HasMaxLength(150);
        builder.Property(warranty => warranty.Notes).HasMaxLength(2000);
        builder.Ignore(warranty => warranty.ExpirationDate);
        builder.HasOne(warranty => warranty.Record).WithOne(record => record.Warranty)
            .HasForeignKey<MaintenanceWarranty>(warranty => warranty.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

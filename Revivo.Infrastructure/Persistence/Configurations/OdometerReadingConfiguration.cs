using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class OdometerReadingConfiguration : IEntityTypeConfiguration<OdometerReading>
{
    public void Configure(EntityTypeBuilder<OdometerReading> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint("CK_OdometerReadings_MileageKm", "[MileageKm] >= 0"));
        builder.HasOne(reading => reading.Vehicle).WithMany(vehicle => vehicle.OdometerReadings)
            .HasForeignKey(reading => reading.VehicleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(reading => new { reading.VehicleId, reading.RecordedAt, reading.Id })
            .IsDescending(false, true, true);
    }
}

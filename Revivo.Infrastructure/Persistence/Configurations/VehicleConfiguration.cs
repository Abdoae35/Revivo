using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Vehicles_Year", "[Year] >= 1886 AND [Year] <= YEAR(GETUTCDATE()) + 1");
            table.HasCheckConstraint("CK_Vehicles_CurrentMileageKm", "[CurrentMileageKm] >= 0");
        });
        builder.Property(vehicle => vehicle.Nickname).HasMaxLength(100);
        builder.Property(vehicle => vehicle.ImageUrl).HasMaxLength(2048);
        builder.HasIndex(vehicle => vehicle.OwnerId);
        builder.HasIndex(vehicle => vehicle.CarModelId);
        builder.HasOne<CarModel>().WithMany().HasForeignKey(vehicle => vehicle.CarModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

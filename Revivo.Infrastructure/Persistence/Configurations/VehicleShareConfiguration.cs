using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Identity;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class VehicleShareConfiguration : IEntityTypeConfiguration<VehicleShare>
{
    public void Configure(EntityTypeBuilder<VehicleShare> builder)
    {
        builder.Property(share => share.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(share => new { share.VehicleId, share.InvitedUserId }).IsUnique()
            .HasFilter("[Status] IN (N'Pending', N'Accepted')");
        builder.HasIndex(share => new { share.InvitedUserId, share.Status });
        builder.HasOne(share => share.Vehicle).WithMany(vehicle => vehicle.Shares)
            .HasForeignKey(share => share.VehicleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(share => share.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(share => share.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

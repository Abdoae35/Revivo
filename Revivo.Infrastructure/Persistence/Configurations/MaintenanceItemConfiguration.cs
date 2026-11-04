using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceItemConfiguration : IEntityTypeConfiguration<MaintenanceItem>
{
    public void Configure(EntityTypeBuilder<MaintenanceItem> builder)
    {
        builder.ToTable("MaintenanceItems");
        builder.Property(item => item.Name).IsRequired().HasMaxLength(100)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        builder.HasIndex(item => new { item.MaintenanceCategoryId, item.Name }).IsUnique();
        builder.HasOne(item => item.Category).WithMany(category => category.Items)
            .HasForeignKey(item => item.MaintenanceCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

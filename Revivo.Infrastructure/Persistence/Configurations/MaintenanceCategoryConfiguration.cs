using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceCategoryConfiguration : IEntityTypeConfiguration<MaintenanceCategory>
{
    public void Configure(EntityTypeBuilder<MaintenanceCategory> builder)
    {
        builder.ToTable("MaintenanceCategories");
        builder.Property(category => category.Name).IsRequired().HasMaxLength(100)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");
        builder.HasIndex(category => category.Name).IsUnique();
    }
}

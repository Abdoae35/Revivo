using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class CarMakeConfiguration : IEntityTypeConfiguration<CarMake>
{
    public void Configure(EntityTypeBuilder<CarMake> builder)
    {
        builder.Property(make => make.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(make => make.Name).IsUnique();
    }
}

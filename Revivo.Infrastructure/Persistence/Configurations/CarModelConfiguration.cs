using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class CarModelConfiguration : IEntityTypeConfiguration<CarModel>
{
    public void Configure(EntityTypeBuilder<CarModel> builder)
    {
        builder.Property(model => model.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(model => new { model.CarMakeId, model.Name }).IsUnique();
        builder.HasOne<CarMake>().WithMany().HasForeignKey(model => model.CarMakeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

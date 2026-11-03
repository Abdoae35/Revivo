using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence.Configurations;

public sealed class CarModelConfiguration : IEntityTypeConfiguration<CarModel>
{
    public void Configure(EntityTypeBuilder<CarModel> builder)
    {
        builder.Property(model => model.Name).HasMaxLength(100).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("CK_CarModels_Years", "([YearStart] IS NULL OR [YearStart] >= 1886) AND ([YearEnd] IS NULL OR [YearEnd] >= [YearStart])"));
        builder.HasIndex(model => new { model.CarMakeId, model.Name }).IsUnique();
        builder.HasOne(model => model.CarMake).WithMany(make => make.Models)
            .HasForeignKey(model => model.CarMakeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

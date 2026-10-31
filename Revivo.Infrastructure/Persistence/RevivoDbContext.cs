using Microsoft.EntityFrameworkCore;
using Revivo.Domain.Entities;

namespace Revivo.Infrastructure.Persistence;

public sealed class RevivoDbContext(DbContextOptions<RevivoDbContext> options) : DbContext(options)
{
    public DbSet<CarMake> CarMakes => Set<CarMake>();
    public DbSet<CarModel> CarModels => Set<CarModel>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RevivoDbContext).Assembly);
    }
}

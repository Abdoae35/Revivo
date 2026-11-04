using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Revivo.Infrastructure.Persistence;
using Revivo.Infrastructure.Identity;
using Revivo.Application.Authentication;
using Revivo.Application.Vehicles;
using Revivo.Infrastructure.Vehicles;
using Revivo.Infrastructure.Catalog;
using Revivo.Infrastructure.Images;
using Revivo.Application.Maintenance;
using Revivo.Infrastructure.Maintenance;

namespace Revivo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RevivoDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("RevivoDb")
                ?? throw new InvalidOperationException("Connection string 'RevivoDb' is not configured.");
            options.UseSqlServer(connectionString);
        });

        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<RevivoDbContext>();

        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ILoginService, LoginService>();

        services.AddScoped<ICarCatalogService, CarCatalogService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<CarCatalogSeeder>();
        services.AddMemoryCache();
        services.AddHttpClient("Unsplash", client =>
        {
            client.BaseAddress = new Uri("https://api.unsplash.com/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<ICarImageService, UnsplashCarImageService>();

        return services;
    }
}

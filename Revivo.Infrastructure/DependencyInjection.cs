using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Revivo.Infrastructure.Persistence;
using Revivo.Infrastructure.Identity;
using Revivo.Application.Authentication;

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

        return services;
    }
}

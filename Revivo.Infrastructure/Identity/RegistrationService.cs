using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Revivo.Application.Authentication;

namespace Revivo.Infrastructure.Identity;

public sealed class RegistrationService(UserManager<ApplicationUser> users) : IRegistrationService
{
    public async Task<RegistrationResult> RegisterAsync(string name, string email, string password)
    {
        if (await users.FindByEmailAsync(email) is not null)
            return new(null, true, []);

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = name, Email = email, UserName = email };
        IdentityResult result;
        try
        {
            result = await users.CreateAsync(user, password);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            if (await users.FindByEmailAsync(email) is not null)
                return new(null, true, []);
            throw;
        }

        if (result.Succeeded)
            return new(user.Id, false, []);

        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            return new(null, true, []);

        return new(null, false, result.Errors.Select(error => error.Description).ToArray());
    }
}

using Microsoft.AspNetCore.Identity;
using Revivo.Application.Authentication;

namespace Revivo.Infrastructure.Identity;

public sealed class LoginService(UserManager<ApplicationUser> users) : ILoginService
{
    public async Task<UserProfile?> GetUserAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is null ? null : new UserProfile(user.Id, user.Name, user.Email);
    }

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null || await users.IsLockedOutAsync(user) || !await users.CheckPasswordAsync(user, password))
            return null;

        return new AuthenticatedUser(user.Id, user.Email!);
    }
}

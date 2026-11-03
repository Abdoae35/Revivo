namespace Revivo.Application.Authentication;

public interface ILoginService
{
    Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password);
    Task<UserProfile?> GetUserAsync(Guid userId);
}

public sealed record AuthenticatedUser(Guid Id, string Email);

public sealed record UserProfile(Guid Id, string? Name, string? Email);

namespace Revivo.Application.Authentication;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(string name, string email, string password);
}

public sealed record RegistrationResult(Guid? UserId, bool DuplicateEmail, IReadOnlyList<string> Errors);

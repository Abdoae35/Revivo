using Microsoft.AspNetCore.Identity;

namespace Revivo.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    // Nullable for accounts created before registration collected a name.
    public string? Name { get; set; }
}

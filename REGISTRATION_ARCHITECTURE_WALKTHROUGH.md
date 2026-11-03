# User registration architecture walkthrough

This walkthrough describes the registration implementation present in the workspace. The C# blocks below are copied from the actual project files; framework internals are explained in prose because their source code is not part of this project. This is a code walkthrough, not evidence that the migration has been applied or that SQL Server connectivity has been verified.

## HTTP request and API layer

ASP.NET Core routes `POST /api/auth/register` to the minimal API handler. Its JSON body is bound to `RegisterRequest`; the record and the endpoint are in the same file. `Name`, `Email`, and `Password` are nullable in the DTO so the handler can report missing input. No example credentials are included here.

### `Revivo.Api/Program.cs`

**Responsibility:** creates the application, registers OpenAPI and Infrastructure services, configures the pipeline, and maps registration.

**Call to the next step:** `builder.Services.AddInfrastructure(builder.Configuration)` registers the implementation that will be injected into the handler. After building the app, `app.MapRegistrationEndpoints()` maps the HTTP route. The registration call passes the application's configuration object; it does not itself open a database connection.

```csharp
using Revivo.Infrastructure;
using Revivo.Api.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapRegistrationEndpoints();

app.Run();
```

### `Revivo.Api/Authentication/RegistrationEndpoints.cs`

**Responsibility:** declares the request DTO, maps the endpoint, checks input, and converts the registration result into an HTTP response.

**Call to the next layer:** `MapRegistrationEndpoints` maps `RegisterAsync` to the POST route. ASP.NET Core supplies `RegisterRequest` from the request body and resolves `IRegistrationService` from dependency injection. After checking the fields, `RegisterAsync` passes trimmed name and email and the supplied password to `registrations.RegisterAsync(...)`.

**Data and responses:** missing or invalid input produces a validation problem (400). A result marked `DuplicateEmail` produces 409 and a message. Other service errors produce a validation problem under `Password` (400). Success produces JSON containing the user ID with status 201. The response does not return the password or its hash. The handler does not itself enforce the full Identity password policy; that is handled by Identity in the Infrastructure step.

```csharp
using System.ComponentModel.DataAnnotations;
using Revivo.Application.Authentication;

namespace Revivo.Api.Authentication;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", RegisterAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, IRegistrationService registrations)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            errors[nameof(request.Name)] = ["Name is required and must not exceed 100 characters."];
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 256 ||
            !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            errors[nameof(request.Email)] = ["A valid email address of at most 256 characters is required."];
        if (string.IsNullOrEmpty(request.Password))
            errors[nameof(request.Password)] = ["Password is required."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var result = await registrations.RegisterAsync(request.Name!.Trim(), request.Email!.Trim(), request.Password!);
        if (result.DuplicateEmail)
            return Results.Conflict(new { message = "An account with this email already exists." });
        if (result.Errors.Count > 0)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Password"] = result.Errors.ToArray() });

        return Results.Json(new { id = result.UserId }, statusCode: StatusCodes.Status201Created);
    }
}

public sealed record RegisterRequest(string? Name, string? Email, string? Password);
```

## Application layer

The Application layer defines the contract used by the API. It contains no Identity or EF Core reference in its project file.

### `Revivo.Application/Authentication/IRegistrationService.cs`

**Responsibility:** defines `IRegistrationService` and the result returned to the caller. `RegistrationResult` carries an optional user ID, a duplicate-email flag, and a list of error descriptions.

**Call to the next layer:** the interface contains no executable implementation. At runtime, dependency injection binds it to Infrastructure's `RegistrationService`; the API's `registrations.RegisterAsync(...)` therefore invokes that implementation. The name, email, and password cross the boundary as method arguments, and the result crosses back to the API.

```csharp
namespace Revivo.Application.Authentication;

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(string name, string email, string password);
}

public sealed record RegistrationResult(Guid? UserId, bool DuplicateEmail, IReadOnlyList<string> Errors);
```

## Domain layer

Registration does **not** directly call a `Revivo.Domain` entity or service. The existing Domain entities model cars and vehicles, not users. The registration contract remains in Application, while the Identity-specific user type and persistence implementation live in Infrastructure. There is therefore no registration-specific Domain C# method to quote or call in this flow.

## Infrastructure layer: Identity and registration

### `Revivo.Infrastructure/Identity/ApplicationUser.cs`

**Responsibility:** defines the user type used by Identity. It inherits `IdentityUser<Guid>` for Identity's GUID key and account properties, and adds the nullable `Name` property. The comment explains why the property is nullable for pre-existing accounts.

**Call to the next step:** this class contains no method that calls another layer. `RegistrationService.RegisterAsync` constructs an `ApplicationUser`; Identity and its EF store then operate on that object.

```csharp
using Microsoft.AspNetCore.Identity;

namespace Revivo.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    // Nullable for accounts created before registration collected a name.
    public string? Name { get; set; }
}
```

### `Revivo.Infrastructure/Identity/RegistrationService.cs`

**Responsibility:** implements the Application registration contract using ASP.NET Core Identity's `UserManager<ApplicationUser>`.

**Calls and data:** `users.FindByEmailAsync(email)` checks for an existing user. The method then creates an `ApplicationUser` with a new GUID and sets `Name`, `Email`, and `UserName`; this implementation uses the email as the username. `users.CreateAsync(user, password)` passes the user and plaintext input password to Identity. Identity validates the account and password using its configured validators, hashes an accepted password using its password hasher, and invokes the configured user store to persist the user. That framework behavior is not implemented as C# source in this project.

**Errors and response data:** if the first lookup finds a user, the service marks the email as duplicate. It also handles Identity's `DuplicateEmail` and `DuplicateUserName` errors. A SQL unique-key exception with number 2601 or 2627 causes another email lookup; if the email is found, it reports a duplicate, otherwise it rethrows. Successful creation returns the GUID; other Identity errors are returned as descriptions. The `try`/`catch` handles a possible conflict during `CreateAsync`, not all database failures.

```csharp
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
```

### `Revivo.Infrastructure/DependencyInjection.cs`

**Responsibility:** wires the existing SQL Server context, Identity and registration implementation into dependency injection.

**Calls and data:** `AddDbContext<RevivoDbContext>` obtains the named `RevivoDb` connection string from configuration and passes it to `UseSqlServer`. The actual value is not embedded in the source shown here. `AddIdentityCore<ApplicationUser>` sets Identity's `RequireUniqueEmail` option, adds GUID roles, and `AddEntityFrameworkStores<RevivoDbContext>()` registers EF-backed Identity stores. `AddScoped<IRegistrationService, RegistrationService>()` tells DI what to supply to the API handler. The store uses the registered context; this registration does not apply migrations or connect to SQL Server by itself.

```csharp
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
```

### `Revivo.Infrastructure/Persistence/RevivoDbContext.cs`

**Responsibility:** provides the EF Core context for both the vehicle model and ASP.NET Core Identity. It inherits `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and calls `base.OnModelCreating(modelBuilder)` to configure Identity's model. It maps `ApplicationUser.Name` with a maximum length of 100 and applies configurations from the Infrastructure assembly.

**Call to the next step:** the `AddEntityFrameworkStores<RevivoDbContext>()` registration gives Identity an EF-backed store using this context. When Identity creates a user, that store uses the context to persist the user to SQL Server. The project does not contain a hand-written `SaveChangesAsync` call for registration: that work is performed within the Identity EF store and EF Core libraries.

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Identity;

namespace Revivo.Infrastructure.Persistence;

public sealed class RevivoDbContext(DbContextOptions<RevivoDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<CarMake> CarMakes => Set<CarMake>();
    public DbSet<CarModel> CarModels => Set<CarModel>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>().Property(user => user.Name).HasMaxLength(100);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RevivoDbContext).Assembly);
    }
}
```

### Identity password hashing and EF persistence

1. `RegistrationService.RegisterAsync` passes the request password to `UserManager<ApplicationUser>.CreateAsync(user, password)`; the service does not implement its own hash algorithm.
2. ASP.NET Core Identity uses its configured password validators and password hasher. If validation succeeds, the Identity user store receives the user with its `PasswordHash` populated. The default Identity password hasher is framework code, not source defined in this repository.
3. The EF-backed Identity store registered by `AddEntityFrameworkStores<RevivoDbContext>()` persists the Identity user through `RevivoDbContext`. The context's SQL Server provider sends the resulting database operation to SQL Server. The model maps Identity users to `AspNetUsers`; the stored password representation is the hash, not the request's plaintext password.
4. The result of `CreateAsync` returns to `RegistrationService`, which converts it to `RegistrationResult`. The API converts that to the HTTP status and body.

The code does not show a custom password policy or a custom password hasher; Identity defaults apply unless configuration elsewhere changes them. A successful build or the presence of the migration files does not prove that an actual database write has occurred.

## SQL Server schema represented by project migrations

The following is the exact `AspNetUsers` creation block and its email/username index blocks from `Revivo.Infrastructure/Persistence/Migrations/20261103154622_AddIdentity.cs`. The full migration also creates other Identity tables; only the registration-relevant user table and indexes are excerpted here.

```csharp
            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
```

The migration defines `PasswordHash` and the GUID primary key. `EmailIndex` is a normal index; `UserNameIndex` is unique when the normalized username is not null. The registration code sets `UserName` to the email and also enables Identity's unique-email validation. These are distinct pieces of the duplicate-email behavior.

### `Revivo.Infrastructure/Persistence/Migrations/20261103155646_AddUserName.cs`

**Responsibility:** adds the nullable `Name` column to `AspNetUsers`; its `Down` method defines the reverse operation. This migration is a schema definition, not a registration-time method call. Whether it is applied to a particular database must be checked separately.

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "AspNetUsers");
        }
    }
}
```

## Complete execution flow

1. **Startup — `Revivo.Api/Program.cs`:** `AddInfrastructure(builder.Configuration)` registers SQL Server, Identity, and `IRegistrationService`; `MapRegistrationEndpoints()` installs the route.
2. **HTTP and API — `Revivo.Api/Authentication/RegistrationEndpoints.cs`:** ASP.NET Core binds the JSON body to `RegisterRequest` and calls `RegistrationEndpoints.RegisterAsync`. The handler validates fields and calls `IRegistrationService.RegisterAsync` with name, email, and password.
3. **Application — `Revivo.Application/Authentication/IRegistrationService.cs`:** the interface defines the call and `RegistrationResult`; it has no implementation of its own. DI resolves the interface to Infrastructure's scoped `RegistrationService`.
4. **Domain — `Revivo.Domain`:** no registration-specific entity or method is invoked.
5. **Infrastructure — `Revivo.Infrastructure/Identity/RegistrationService.cs`:** `RegisterAsync` calls `UserManager<ApplicationUser>.FindByEmailAsync`, constructs `ApplicationUser`, then calls `UserManager<ApplicationUser>.CreateAsync(user, password)`.
6. **Identity and EF Core — framework libraries configured by `Revivo.Infrastructure/DependencyInjection.cs` and `Revivo.Infrastructure/Persistence/RevivoDbContext.cs`:** Identity validates and hashes the password and uses its EF store and the configured context to save the user. SQL Server receives the operation for `AspNetUsers`; the repository's Identity and name migrations describe the relevant table and column.
7. **Return path — Infrastructure → Application contract → API:** the service returns `RegistrationResult`; the handler returns a validation error, conflict, or 201 JSON response to the client.


# Revivo setup result

Status: architecture and configuration code added; package restore, complete build, and SQL connection remain unverified or blocked in this environment. No migrations, business entities, tables, containers, or volumes were created.

## Project structure

```text
Revivo.slnx
Revivo.Api/                      API folder (renamed after setup)
  Revivo.Api.csproj              renamed existing project
  Program.cs
  Revivo.http
  Properties/launchSettings.json
  appsettings.json
  appsettings.Development.json
Revivo.Domain/Revivo.Domain.csproj
Revivo.Application/Revivo.Application.csproj
Revivo.Infrastructure/
  Revivo.Infrastructure.csproj
  DependencyInjection.cs
  Persistence/RevivoDbContext.cs
```

Project references were verified with `dotnet list ... reference`:

- API → Application, Infrastructure
- Application → Domain
- Infrastructure → Application, Domain
- Domain → none

The weather forecast endpoint and its sample HTTP request were removed. The HTTP sample now requests the development OpenAPI document.

## Dependencies and configuration

The project files declare `Microsoft.EntityFrameworkCore.SqlServer` **10.0.5** in Infrastructure and `Microsoft.EntityFrameworkCore.Design` **10.0.5** as a private API startup dependency. The existing `Microsoft.AspNetCore.OpenApi` **10.0.8** remains. Package restore did not complete, so these declarations are not a successful installation verification.

`RevivoDbContext` and `AddInfrastructure` are in Infrastructure; the API calls the extension and reads `ConnectionStrings:RevivoDb` from normal ASP.NET Core configuration, including User Secrets in Development. The API project now has a User Secrets ID. No connection string or password has been supplied. No database connection is opened merely by registering the context. If the context is requested without a connection string, configuration fails with a clear message.

## Verification

- Solution and project references: verified.
- Restore: **incomplete**. The local cache lacks `Microsoft.Data.SqlClient` 6.1.1 or later; an offline restore reported `NU1102`. It also selected newer cached transitive packages and reported `NU1603` warnings. Restore again using the normal configured NuGet sources when network access is available.
- Build: **failed**, because the incomplete restore could not resolve `Microsoft.Data.SqlClient`. The Domain and Application projects built; the complete solution has not been verified. The build also surfaced an existing `Microsoft.OpenApi` 2.0.0 vulnerability warning (NU1903); review it after a fresh online restore.
- Docker: socket access was denied by this execution environment. The container list and mapped port could not be inspected. This does not mean Docker or SQL Server is down.
- SQL Server connection and `SELECT 1`: **unverified**. Docker access, host port, credentials and local TCP access were unavailable. No database was modified.

## Manual steps on the development Mac

1. Identify the existing SQL Server container and its host port:

   ```sh
   docker ps -a --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}'
   docker port <container-name> 1433/tcp
   ```

   Use the published host port, not the container's internal port. Start an existing suitable container if needed; do not create a replacement solely because this environment could not inspect Docker.

2. From the workspace root, configure the secret without placing its value in shell history or a command argument. Run the following and paste the **complete** connection string into the hidden prompt, replacing the placeholders with the actual port and credentials:

   ```sh
   python3 - <<'PY'
   import getpass, json, subprocess
   value = getpass.getpass('Connection string: ')
   subprocess.run(['dotnet', 'user-secrets', 'set', '--project', 'Revivo.Api/Revivo.Api.csproj'],
                  input=json.dumps({'ConnectionStrings:RevivoDb': value}), text=True, check=True)
   PY
   ```

   Connection string format for a local development container using a self-signed certificate:

   ```text
   Server=localhost,<host-port>;Database=RevivoDb;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=True
   ```

   Enter the actual value only at the hidden prompt. Quote or escape connection-string values as required if credentials contain special characters. `TrustServerCertificate=True` is intended for a trusted local development instance; use validated certificates for production. User Secrets is a development convenience and is not an encrypted secret store.

3. Restore and build with access to the configured NuGet feed:

   ```sh
   dotnet restore Revivo.slnx
   dotnet build Revivo.slnx --no-restore
   ```

4. If `sqlcmd` is available, test the existing database using its host port and SQL login. This prompts without echoing the password and runs only `SELECT 1`:

   ```sh
   read 'REVIVO_SQL_USER?SQL login: '
   read 'REVIVO_SQL_PORT?Published host port: '
   read -s 'SQLCMDPASSWORD?SQL password: '; echo
   export SQLCMDPASSWORD
   sqlcmd -S "localhost,$REVIVO_SQL_PORT" -U "$REVIVO_SQL_USER" -d RevivoDb -C -Q 'SELECT 1'
   unset SQLCMDPASSWORD REVIVO_SQL_USER REVIVO_SQL_PORT
   ```

   The command assumes zsh and a `sqlcmd` version supporting `-C`. If `RevivoDb` does not already exist or the login lacks access, the test cannot succeed; this setup does not create the database. Check the database and login deliberately before making changes.

The globally installed `dotnet-ef` was previously reported as 9.0.2. Align it with EF Core 10 before using EF tooling in a later task. No migration is needed for this setup.

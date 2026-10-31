# Revivo development setup report

Inspection date: 2026-10-31. The inspection was read-only except for creating this report. Docker socket and local network access were restricted in the inspection environment; the Docker and SQL Server results below are therefore incomplete.

## 1. Current project structure

```text
Revivo/
├── Revivo.slnx
├── REVIVO_SETUP_REPORT.md
├── docs/
│   └── PRODUCT_REQUIREMENTS.md
└── Revivo/
    ├── Revivo.csproj
    ├── Program.cs
    ├── Properties/launchSettings.json
    ├── Revivo.http
    ├── appsettings.json
    └── appsettings.Development.json
```

The solution contains one project: `Revivo/Revivo.csproj`, an ASP.NET Core web project targeting **net10.0**. It has no project-to-project references. There are no separate Domain, Application, Infrastructure, or API projects, so the requested Clean Architecture layering is not currently configured. No test project is in the solution.

`Program.cs` uses the modern minimal API hosting model. It registers OpenAPI, exposes the OpenAPI document in Development, enables HTTPS redirection, and maps the template `/weatherforecast` endpoint. It does not register database services or implement car-maintenance functionality.

## 2. Installed .NET environment

- Selected SDK in this directory: **10.0.300**; no `global.json` was found in the workspace to pin an SDK.
- Installed SDK versions: 6.0.417, 6.0.418, 6.0.419, 6.0.420, 6.0.422, 6.0.424, 6.0.425, 7.0.311, 7.0.312, 7.0.313, 7.0.314, 7.0.316, 7.0.317, 8.0.101, 8.0.303, 9.0.305, and 10.0.300.
- Both `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` runtimes are installed for .NET 6, 7, 8, 9, and 10; version **10.0.8** is present for each, supporting the project's target framework.
- Global EF CLI tool: **dotnet-ef 9.0.2**. It is a tool, not an EF Core project dependency, and its major version should be aligned with the EF Core packages chosen for the project.
- The only explicitly declared NuGet dependency is `Microsoft.AspNetCore.OpenApi` **10.0.8**.

## 3. Docker and SQL Server status

- Docker CLI is installed: **28.1.1**.
- Docker daemon status, container inventory, SQL Server/Azure SQL Edge image, container name, status, and published ports **could not be determined**: access to the Docker Unix socket was denied by the inspection environment. This does not establish whether Docker is running.
- A TCP connection attempt to `127.0.0.1:1433` was also denied by the environment, so SQL Server reachability and authentication were **not verified**. This does not establish that SQL Server is down or that port 1433 is closed.
- For an API running directly on macOS, use `localhost,<published-host-port>` as the SQL Server host once the container's published host port is confirmed. For example, if Docker maps host port 1433 to container port 1433, use `localhost,1433`. Do not assume a container name or internal container port is reachable directly from the host.

## 4. EF Core and database configuration

- No EF Core package references, including the SQL Server provider or design-time package, are declared in the project.
- No `DbContext`, `AddDbContext`, `UseSqlServer`, migrations, or database configuration is present in the inspected application files.
- Neither `appsettings.json` nor `appsettings.Development.json` contains a `ConnectionStrings` section. No `ConnectionStrings__...` environment variable was present in the inspection process. Other launch contexts or configuration providers may have different values.
- The project file has no `UserSecretsId`, so .NET User Secrets are not enabled for this project. No secret values were inspected or recorded.

## 5. Missing dependencies and setup

For the stated SQL Server/EF Core architecture, the project still needs an appropriate EF Core SQL Server provider and, if migrations are used, the EF Core design-time package. Select compatible package versions and align the EF CLI major version. The application also needs a context, its registration, a connection string, and migrations when the data model is ready. Clean Architecture projects and their dependency references remain to be designed and added. A test project is not present.

## 6. Problems discovered

1. The solution is a starter minimal API, not yet a layered car-maintenance backend.
2. There is no configured database provider, context, connection string, or migration infrastructure.
3. User Secrets are not enabled for development database credentials.
4. The global EF CLI is version 9 while the API targets .NET 10; review and align it with the EF Core packages selected before using migrations.
5. Docker and database availability cannot be assessed from this restricted session; neither a running container nor a failed server can be inferred.

## 7. Recommended setup steps

1. On the Mac, run `docker info` and `docker ps -a` to verify the daemon and identify any existing SQL Server or Azure SQL Edge container. Check its published ports with `docker port <container-name>`. If no suitable container exists, choose and configure a compatible SQL Server deployment for the Mac's architecture before creating one.
2. Confirm SQL Server responds using an available SQL client and the published host port. Test authentication without printing or sharing credentials.
3. Decide the Clean Architecture project boundaries and reference direction: the API composes the application and infrastructure, while domain and application code remain independent of infrastructure implementation details.
4. Add mutually compatible EF Core SQL Server and design-time packages and align `dotnet-ef` with their major version. Implement and register the context after designing the model.
5. Enable User Secrets for the API project and store the development connection string there, using `localhost,<published-host-port>` for a host-run API. Keep credentials out of source control and use a secure secret store for deployed environments.
6. Add migrations only after reviewing the model and connection target. Apply them deliberately to the intended database, then add tests for the resulting behavior.

No packages were installed, application files modified, containers changed, or database changes made during this inspection.

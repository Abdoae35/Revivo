# EF Core tooling

The solution targets EF Core 10. To align the globally installed `dotnet-ef` tool with the project packages, run:

```sh
dotnet tool update --global dotnet-ef --version 10.0.14
dotnet ef --version
```

After restoring and building the solution, verify the existing migrations without connecting to or changing a database:

```sh
dotnet ef migrations script --project Revivo.Infrastructure --startup-project Revivo.Api --no-build --output /tmp/revivo-migrations.sql
```

Review the generated script before performing any database operation. Generating the script does not apply a migration.

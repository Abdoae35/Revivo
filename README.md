# Revivo

Revivo is an ASP.NET Core Web API for keeping a vehicle's service history, odometer readings, warranties, and maintenance plans together. It also provides driving and expense summaries for a client dashboard. Vehicle owners can give another registered user read-only access without sharing an account.

This repository contains the backend API and tests. It does not contain a Flutter client or a push-notification service.

## Features

- **Accounts and vehicles:** Register and sign in with ASP.NET Core Identity and JWTs. Browse car makes, models, and available years; create and manage owned vehicles. An optional Unsplash-backed lookup supplies an illustrative car image, with a fallback response when no image is available.
- **Maintenance history:** Record service date, item, mileage, notes, and cost in EGP. Browse, update, and remove records for an owned vehicle.
- **Periodic maintenance:** Configure distance and/or time intervals and warning thresholds per maintenance item. Read current and upcoming service status, including due-soon and overdue results.
- **Warranties:** Attach an optional warranty to a maintenance record, then view its expiration status and warning window.
- **Mileage tracking:** Add nondecreasing odometer readings, update the vehicle's current mileage atomically, and browse paginated reading history.
- **Notification preferences and alerts:** Store weekly reminder preferences per user and vehicle, including day, time, and time zone. Read maintenance and warranty alerts computed from the existing status services. Preferences and alerts do not send messages.
- **Statistics and expenses:** Read current, monthly, and annual driving summaries; monthly driving estimates; lifetime and period maintenance costs; category totals; and the most expensive maintenance records. Sparse odometer data is interpolated only between known readings. Periods without enough data are marked unavailable; estimates are labeled.
- **Vehicle sharing:** Invite an existing user by email. The invitee can accept or reject in the application. Accepted users can read vehicle detail, histories, status, alerts, and statistics; the owner retains write and share-management rights. Owners can revoke access.

## Tech stack

| Area | Implementation |
| --- | --- |
| Runtime and API | .NET 10, ASP.NET Core Minimal APIs |
| Persistence | EF Core 10.0.14, SQL Server, EF Core migrations |
| Identity | ASP.NET Core Identity with GUID user IDs, JWT bearer authentication |
| Tests | xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, optional SQL Server integration tests |
| API description | ASP.NET Core OpenAPI endpoint in Development |
| Optional images | Unsplash HTTP API, memory cache |

## Architecture

The solution follows four layers:

| Project | Responsibility |
| --- | --- |
| `Revivo.Domain` | Entities, validation rules, maintenance status, and pure mileage calculations. |
| `Revivo.Application` | Use-case contracts, DTOs, and expense calculations. |
| `Revivo.Infrastructure` | EF Core mappings and migrations, Identity, SQL Server queries, service implementations, catalog import, and image lookup. |
| `Revivo.Api` | Minimal API routes, request validation, JWT configuration, and HTTP responses. |

`Revivo.Api.Tests` covers rules, endpoints, EF mappings, and opt-in SQL integration scenarios. The API returns DTOs rather than EF entities.

## Database design

The SQL Server schema has **11 application tables** and **7 ASP.NET Identity tables**. `CarMakes` → `CarModels` → `Vehicles` identifies a car. `MaintenanceCategories` → `MaintenanceItems` classifies `MaintenanceRecords` and `MaintenanceSchedules`. A record may have one `MaintenanceWarranty`. Vehicles also have `OdometerReadings`, `NotificationSettings`, and `VehicleShares`. Sharing rows reference both the inviting and invited Identity users.

The [database ERD](docs/DATABASE_ERD.md) shows all mapped tables and actual foreign keys. The standalone [Mermaid source](docs/erd.mmd) can be pasted into Mermaid Live Editor. Mileage and expense statistics reuse existing records; they have no separate analytics tables.

## Authentication and authorization

Register or log in to obtain a JWT, then send `Authorization: Bearer <token>` on protected requests. The API reads the user ID from the token's `sub` claim. It does not accept a client-supplied owner ID for access decisions. Registration, login, and catalog browsing are public; all vehicle and invitation routes require authentication.

Owners can change their vehicles, mileage, maintenance, schedules, warranties, notification settings, and shares. An **accepted** share grants read access to the vehicle detail, maintenance and odometer history, maintenance status, warranties, notification settings, alerts, and statistics. Pending and rejected shares grant no access. `GET /api/vehicles` lists owned vehicles; `GET /api/vehicles/shared` lists accepted shared vehicles. Missing or inaccessible vehicles return 404. See the [sharing workflow](docs/VEHICLE_SHARING.md) for invitation and revocation behavior.

## API routes

Routes below show path parameters without the ASP.NET Core `:int` constraint. Unless marked public, every route requires a JWT. The Development environment exposes the generated OpenAPI document at `/openapi/v1.json`.

### Accounts and car catalog

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/auth/register` | Register an account. Public. |
| POST | `/api/auth/login` | Authenticate and obtain a token. Public. |
| GET | `/api/auth/me` | Return the authenticated user. |
| GET | `/api/car-catalog/makes` | Search car makes. Public. |
| GET | `/api/car-catalog/models` | List models for `makeId`, with optional search. Public. |
| GET | `/api/car-catalog/models/{modelId}/years` | List known model years. Public. |
| GET | `/api/car-catalog/car-image` | Look up an illustrative image using `modelId` and `year`. Public. |

### Vehicles, sharing, and odometer

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/vehicles` | Create an owned vehicle. |
| GET | `/api/vehicles` | List owned vehicles. |
| GET | `/api/vehicles/{id}` | Read an owned or accepted shared vehicle. |
| PUT | `/api/vehicles/{id}` | Update an owned vehicle. |
| DELETE | `/api/vehicles/{id}` | Delete an owned vehicle. |
| GET | `/api/vehicles/shared` | List vehicles shared with the current user. |
| POST | `/api/vehicles/{vehicleId}/shares` | Invite a registered user by email; owner only. |
| GET | `/api/vehicles/{vehicleId}/shares` | List a vehicle's shares; owner only. |
| DELETE | `/api/vehicles/{vehicleId}/shares/{shareId}` | Cancel or revoke a share; owner only. |
| GET | `/api/vehicle-invitations` | List invitations received by the current user. |
| POST | `/api/vehicle-invitations/{shareId}/accept` | Accept a pending invitation as its recipient. |
| POST | `/api/vehicle-invitations/{shareId}/reject` | Reject a pending invitation as its recipient. |
| POST | `/api/vehicles/{vehicleId}/odometer` | Add a reading and update current mileage; owner only. |
| GET | `/api/vehicles/{vehicleId}/odometer/history` | Read paginated readings, newest first. |

### Maintenance, schedules, and warranties

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/vehicles/{vehicleId}/maintenance` | Create a maintenance record; owner only. |
| GET | `/api/vehicles/{vehicleId}/maintenance` | Read paginated maintenance history. |
| GET | `/api/vehicles/{vehicleId}/maintenance/{id}` | Read a maintenance record. |
| PUT | `/api/vehicles/{vehicleId}/maintenance/{id}` | Update a record; owner only. |
| DELETE | `/api/vehicles/{vehicleId}/maintenance/{id}` | Delete a record; owner only. |
| PUT | `/api/vehicles/{vehicleId}/maintenance-items/{itemId}/schedule` | Configure a periodic schedule; owner only. |
| GET | `/api/vehicles/{vehicleId}/maintenance-status` | Read calculated service status. |
| GET | `/api/vehicles/{vehicleId}/upcoming-maintenance` | Read upcoming service items. |
| PUT | `/api/vehicles/{vehicleId}/maintenance/{recordId}/warranty` | Create or update a record's warranty; owner only. |
| GET | `/api/vehicles/{vehicleId}/maintenance/{recordId}/warranty` | Read a record's warranty. |
| DELETE | `/api/vehicles/{vehicleId}/maintenance/{recordId}/warranty` | Delete a warranty; owner only. |
| GET | `/api/vehicles/{vehicleId}/warranties` | List warranties and expiration status. |

### Preferences, alerts, and statistics

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/vehicles/{vehicleId}/notification-settings` | Read saved or default preferences. |
| PUT | `/api/vehicles/{vehicleId}/notification-settings` | Save preferences; owner only. |
| GET | `/api/vehicles/{vehicleId}/alerts` | Read maintenance and warranty alerts. |
| GET | `/api/vehicles/{vehicleId}/statistics/overview` | Current mileage, driving, expenses, and operation count. |
| GET | `/api/vehicles/{vehicleId}/statistics/mileage` | Monthly mileage and annual total for optional `year`. |
| GET | `/api/vehicles/{vehicleId}/statistics/expenses` | Monthly costs, categories, and top records for optional `year`. |

## Getting started

### Prerequisites

- .NET SDK 10 and a reachable SQL Server instance.
- `dotnet-ef` version 10.0.14 for database migrations (see [EF Core tooling](docs/EF_CORE_TOOLING.md)).
- Python 3 only if using the signing-key generation command in the [local JWT setup](docs/JWT_DEVELOPMENT_SETUP.md).

From the repository root:

```sh
dotnet restore Revivo.slnx
dotnet build Revivo.slnx --no-restore
```

Configure `ConnectionStrings:RevivoDb` for the API project using .NET User Secrets or a secure environment-specific provider. For local development, one way to set it is:

```sh
dotnet user-secrets set "ConnectionStrings:RevivoDb" "<your SQL Server connection string>" --project Revivo.Api
```

Set a unique `Jwt:SigningKey` of at least 32 UTF-8 bytes using the command in [local JWT setup](docs/JWT_DEVELOPMENT_SETUP.md). `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpirationMinutes` have defaults in `Revivo.Api/appsettings.json`. Keep the signing key and database credentials outside Git.

Apply the existing migrations, then run the API:

```sh
dotnet ef database update --project Revivo.Infrastructure --startup-project Revivo.Api
dotnet run --project Revivo.Api --launch-profile https
```

The HTTPS launch profile listens at `https://localhost:7076` and also configures `http://localhost:5269`. OpenAPI is available only in Development. The car catalog requires data to be useful; see [catalog data](docs/CAR_CATALOG_DATA.md) for the optional CSV import workflow. [Car images](docs/CAR_IMAGES.md) explains optional Unsplash configuration and fallback behavior.

## Testing

Run the normal suite from the repository root:

```sh
dotnet test Revivo.slnx --no-restore
```

SQL integration tests are **opt-in**. With a reachable migrated SQL Server and the API project's `ConnectionStrings:RevivoDb` user secret configured, run:

```sh
dotnet test Revivo.Api.Tests/Revivo.Api.Tests.csproj --no-restore -e REVIVO_SQL_INTEGRATION=1
```

Without `REVIVO_SQL_INTEGRATION=1`, SQL test methods return before contacting the database; a passing default suite does not prove SQL integration. The integration fixtures use transactions and roll back their inserted data.

## Postman collection

Import the single project collection at [`postman/Revivo.postman_collection.json`](postman/Revivo.postman_collection.json). Set `baseUrl` for your running API, run registration or login, and use the resulting `accessToken` for protected requests. Set `vehicleId` after creating a vehicle. Sharing scenarios also use a second registered account and the collection's `otherAccessToken` and invitee variables. The collection contains example requests and response checks for each feature; its committed values contain no real tokens or passwords.

## Repository structure

```text
Revivo.Domain/          Entities and domain calculations
Revivo.Application/     DTOs, service contracts, expense calculations
Revivo.Infrastructure/  EF Core, Identity, SQL Server services, migrations
Revivo.Api/             Minimal API endpoints and configuration
Revivo.Api.Tests/       Unit, endpoint, mapping, and SQL integration tests
postman/                Revivo Postman collection
docs/                   Setup guides, sharing workflow, and database ERD
```

## Current status

The repository implements the backend features described above, including vehicle sharing and read-only shared access. Notification settings and alert retrieval are available; scheduled reminder delivery, Firebase/push delivery, and email invitations are not implemented. The Flutter dashboard is a separate future client. An external car catalog CSV and optional Unsplash credentials are needed only for their respective integrations.

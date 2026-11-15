# Project History

## Current state

The current checkout is `master`, tracking `origin/master`, with vehicle sharing included in commit `f72e91e`. The public README and Mermaid ERD document the implemented backend and the latest EF Core snapshot. The default suite passes 71/71. SQL integration tests remain opt-in and need a reachable configured SQL Server.

## Operations

### 2026-11-15 — Document the completed backend and database schema

- Audited the solution projects, entities, EF Core snapshot and migrations, API route mappings, authorization, tests, and the single Postman collection. The latest migration is `20261115145958_AddVehicleShares`; no migration or application code was changed.
- Created root `README.md` with verified feature, architecture, API, setup, security, testing, Postman, and project-status sections. Its 39 documented routes match the 39 mapped routes.
- Created `docs/DATABASE_ERD.md` and standalone `docs/erd.mmd` with 18 EF-mapped tables (11 application, 7 Identity) and 19 foreign-key relationships. The Markdown and standalone Mermaid diagrams are identical. The ERD distinguishes user GUID fields without database foreign keys from actual Identity references.
- Refreshed this history and the bounded `AGENTS.md` handoff, replacing stale uncommitted feature-branch status.
- Verification: `dotnet build Revivo.slnx --no-restore -m:1 -nr:false` succeeded with zero warnings and errors; `dotnet test Revivo.slnx --no-build --no-restore -m:1 -nr:false` passed 71/71. The standard restore stalled while resolving packages in the restricted environment; a local-cache restore was also attempted. SQL integration tests were not run in this documentation pass.
- Git: the documentation work was performed on the checked-out `master` branch without switching or merging branches; commit and push status is recorded in the final report.

### 2026-11-15 — Implement vehicle sharing and access management

- Added `VehicleShare` with Pending, Accepted, and Rejected states; EF configuration and additive migration `20261115145958_AddVehicleShares` add vehicle/user foreign keys and a filtered unique index preventing duplicate pending or accepted shares.
- Added owner invitation, share listing and revocation; invited-user inbox, accept and reject; and accepted shared-vehicle listing. Email is used to find a registered user, with no email delivery. User identity for authorization comes from JWT claims.
- Centralized accepted-share read checks in `VehicleAccess` and applied them to vehicle detail, maintenance history, periodic status, warranty reads, odometer history, alerts, and statistics. Existing writes remain owner-only. Added workflow and permission documentation in `docs/VEHICLE_SHARING.md`.
- Updated the existing Postman collection with 20 sharing workflow and denial requests, validation scripts, and sample responses. No real credentials were added.
- Verification: restored all projects from the local NuGet cache after the default sandboxed restore stalled; `dotnet build Revivo.slnx --no-restore -m:1 -nr:false` succeeded with no warnings/errors; default tests passed 71/71; SQL-enabled tests passed 71/71 using `dotnet test ... -e REVIVO_SQL_INTEGRATION=1`. EF applied the migration. The SQL fixture rolled back its user, vehicle, maintenance, and share records.
- The first SQL-enabled test exposed an EF translation error in invitation sorting; sorting was moved before DTO projection and the full SQL-enabled suite passed on rerun. The environment-prefixed test command was denied a local socket by the sandbox; the supported `dotnet test -e` option ran successfully.
- Commit/push: `git add` succeeded, but `git commit -m "Add vehicle share model and persistence service"` failed with `Unable to create '/Users/macbookpro/Revivo/.git/index.lock': Operation not permitted`. The session approval policy does not allow escalation. No commit was created or pushed.
- Remaining work: commit and push the feature on `feature/vehicle-sharing` once Git index writes are available.

### 2026-11-15 — Implement statistics and expenses

- Added pure `MileageAnalyticsCalculator` in Domain. It linearly interpolates between UTC readings, never extrapolates beyond the first or last reading, marks estimated and unavailable periods, and marks the last covered month/year as partial. Annual distance is calculated directly at period boundaries to avoid summing rounded chart values.
- Added pure `ExpenseAnalyticsCalculator` and statistics contracts in Application. Expenses use maintenance dates and decimal EGP costs; annual responses include 12 months, category totals, five largest records, and operation count.
- Added `StatisticsService` in Infrastructure and three JWT-protected routes in API: `/api/vehicles/{vehicleId}/statistics/overview`, `/mileage`, and `/expenses`. All queries filter by authenticated vehicle owner. No database tables or migrations were added.
- Added nine requests and response scripts in the existing Postman collection under Statistics & Expenses. Added calculation, endpoint, and SQL integration tests; the SQL fixture inserts temporary data inside a transaction and rolls it back. Corrected the existing notification SQL test's environment gate because its dynamic skip was reported as a failure by this xUnit runner.
- Verification: `dotnet restore Revivo.slnx` succeeded; `dotnet build Revivo.slnx --no-restore -m:1 -nr:false` succeeded with zero warnings/errors; default `dotnet test Revivo.slnx --no-restore -m:1 -nr:false` passed 67/67; SQL-enabled `REVIVO_SQL_INTEGRATION=1 dotnet test Revivo.Api.Tests/Revivo.Api.Tests.csproj --no-build --no-restore -m:1 -nr:false` passed 67/67. The SQL fixture caught a 0.02 km annual rounding error, which was corrected before the passing rerun. `git diff --check` passed.
- The sandbox denied the SQL test runner's local socket; unrestricted execution passed. No persistent fixture data was left in SQL Server.
- Commit/push: pure analytics `f1a17ed` and API/Postman `a42a70b` pushed to `origin/feature/statistics-expenses`; this status update is documented in a follow-up commit.
- Remaining work: none for the requested API. A Flutter dashboard can consume the current endpoints.

### 2026-11-15 — Implement notification settings and alerts

- Added `NotificationSettings` domain rules, application contracts and alert selection, EF mapping, service, and migration `20261115135625_AddNotificationSettings`. A unique index enforces one row per user and vehicle; GET supplies defaults without inserting a row.
- Added JWT-protected GET/PUT `/api/vehicles/{vehicleId}/notification-settings` and GET `/api/vehicles/{vehicleId}/alerts`. The alert service reuses periodic maintenance and warranty services and respects enabled preferences.
- Added nine requests with response scripts under Notification Settings & Alerts in the existing `postman/Revivo.postman_collection.json`.
- Added domain, EF mapping, endpoint, alert selection, and opt-in SQL integration tests. The SQL test writes a temporary vehicle and settings inside a transaction and rolls it back.
- Verification: `dotnet restore Revivo.slnx` succeeded; `dotnet build Revivo.slnx --no-restore` succeeded with no warnings or errors; `REVIVO_SQL_INTEGRATION=1 dotnet test Revivo.Api.Tests/Revivo.Api.Tests.csproj --no-restore -m:1 -nr:false` passed 54/54 with no skips. EF applied the additive migration and listed all nine migrations as applied. `git diff --check` passed.
- A draft integration test first failed to compile because this xUnit version lacks `Assert.Skip`; it was corrected. The sandboxed test runner was denied a local socket; the same test command passed with unrestricted execution.
- Commit/push: implementation committed as `0ff666b` and pushed to `origin/feature/notifications`; this history status update is documented in a follow-up commit.
- Remaining work: none for settings and alert retrieval. Push delivery awaits the separate Flutter/Firebase feature.

### 2026-11-15 — Implement mileage tracking

- Added `OdometerReading` domain entity, application contracts, EF mapping, service, and migration `20261115132509_AddOdometerReadings`; the service updates vehicle mileage and inserts a reading in one transaction with a conditional update that rejects decreases.
- Added authenticated POST `/api/vehicles/{vehicleId}/odometer` and paginated GET `/api/vehicles/{vehicleId}/odometer/history`. Existing periodic maintenance endpoints read current mileage and use the existing calculator, so they reflect committed updates.
- Added the seven requested requests and response scripts to the existing `postman/Revivo.postman_collection.json` under Mileage Tracking. Added endpoint, domain, mapping, and calculator tests in `Revivo.Api.Tests/OdometerTests.cs`.
- Verification: `dotnet restore Revivo.slnx` succeeded; `dotnet build Revivo.slnx --no-restore` succeeded with no warnings or errors; `dotnet test Revivo.slnx --no-restore` passed 50/50 tests; `git diff --check` passed. SQL Server integration and data persistence were not tested.
- Database application: `dotnet ef database update --project Revivo.Infrastructure --startup-project Revivo.Api --no-build` failed both in the sandbox and with unrestricted network access: SQL Server connection refused (error 10061). No database reset or data removal was attempted.
- Commit/push: implementation committed as `dc0e7ac` and pushed to `origin/feature/mileage-notifications`; this history status update is documented in a follow-up commit.
- Remaining work: apply the migration and run SQL Server integration checks when the configured server is reachable.

### 2026-11-15 — Remove the tast skill from the application repo

- Removed `.agents/skills/design-taste-frontend/` and its entry from `skills-lock.json`.
- Removed the skill-specific usage instructions from `docs/PRODUCT_REQUIREMENTS.md`; retained its visual design direction as ordinary product guidance.
- Updated `AGENTS.md` with the current handoff.
- The initial sandboxed directory removal was denied by file permissions; the requested removal completed after filesystem escalation.
- Verification: searched for active skill references (only this history and the handoff mention the removed skill), reviewed Git status, and ran `git diff --check` successfully. No tests were run because this change only removes documentation and skill assets.
- Commit/push: not performed.
- Remaining work: none.

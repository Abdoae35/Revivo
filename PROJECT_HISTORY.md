# Project History

## Current state

Notification settings and alert aggregation are implemented on `feature/notifications`. Migration `20261115135625_AddNotificationSettings` is applied to the configured SQL Server. EF reports the earlier odometer migration as applied as well. Firebase and push delivery are not implemented.

## Operations

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

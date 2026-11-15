# Project History

## Current state

Mileage tracking code and Postman requests are in place on `feature/mileage-notifications`. Migration `20261115132509_AddOdometerReadings` is generated but has not been applied because SQL Server refused the connection. The previous skill removal remains in effect.

## Operations

### 2026-11-15 — Implement mileage tracking

- Added `OdometerReading` domain entity, application contracts, EF mapping, service, and migration `20261115132509_AddOdometerReadings`; the service updates vehicle mileage and inserts a reading in one transaction with a conditional update that rejects decreases.
- Added authenticated POST `/api/vehicles/{vehicleId}/odometer` and paginated GET `/api/vehicles/{vehicleId}/odometer/history`. Existing periodic maintenance endpoints read current mileage and use the existing calculator, so they reflect committed updates.
- Added the seven requested requests and response scripts to the existing `postman/Revivo.postman_collection.json` under Mileage Tracking. Added endpoint, domain, mapping, and calculator tests in `Revivo.Api.Tests/OdometerTests.cs`.
- Verification: `dotnet restore Revivo.slnx` succeeded; `dotnet build Revivo.slnx --no-restore` succeeded with no warnings or errors; `dotnet test Revivo.slnx --no-restore` passed 50/50 tests; `git diff --check` passed. SQL Server integration and data persistence were not tested.
- Database application: `dotnet ef database update --project Revivo.Infrastructure --startup-project Revivo.Api --no-build` failed both in the sandbox and with unrestricted network access: SQL Server connection refused (error 10061). No database reset or data removal was attempted.
- Commit/push: pending at this checkpoint.
- Remaining work: apply the migration and run SQL Server integration checks when the configured server is reachable.

### 2026-11-15 — Remove the tast skill from the application repo

- Removed `.agents/skills/design-taste-frontend/` and its entry from `skills-lock.json`.
- Removed the skill-specific usage instructions from `docs/PRODUCT_REQUIREMENTS.md`; retained its visual design direction as ordinary product guidance.
- Updated `AGENTS.md` with the current handoff.
- The initial sandboxed directory removal was denied by file permissions; the requested removal completed after filesystem escalation.
- Verification: searched for active skill references (only this history and the handoff mention the removed skill), reviewed Git status, and ran `git diff --check` successfully. No tests were run because this change only removes documentation and skill assets.
- Commit/push: not performed.
- Remaining work: none.

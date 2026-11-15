# Revivo project guidance

Maintain one project-wide Postman collection at `postman/Revivo.postman_collection.json`. When implementing or changing an API endpoint, update that collection and organize requests under the appropriate feature folder. Keep credentials and real secrets out of the collection and its environment files.

## Current handoff

Vehicle sharing and read-only shared access are implemented on `feature/vehicle-sharing`. Migration `20261115145958_AddVehicleShares` was applied to the configured SQL Server, and the SQL integration test uses a rolled-back fixture. The changes are uncommitted because Git cannot create `.git/index.lock` under current permissions; commit and push when Git writes are available. See [PROJECT_HISTORY.md](PROJECT_HISTORY.md) and [docs/VEHICLE_SHARING.md](docs/VEHICLE_SHARING.md).

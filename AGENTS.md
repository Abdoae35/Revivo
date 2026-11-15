# Revivo project guidance

Maintain one project-wide Postman collection at `postman/Revivo.postman_collection.json`. When implementing or changing an API endpoint, update that collection and organize requests under the appropriate feature folder. Keep credentials and real secrets out of the collection and its environment files.

## Current handoff

The current checkout is `master` at the vehicle-sharing implementation. The root [README.md](README.md) documents all 39 API routes and the [database ERD](docs/DATABASE_ERD.md) reflects the latest EF Core snapshot (18 tables). The default test suite passed 71/71 during the documentation audit; SQL integration tests require `REVIVO_SQL_INTEGRATION=1` and a reachable configured SQL Server. See [PROJECT_HISTORY.md](PROJECT_HISTORY.md) for verification and Git status.

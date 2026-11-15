# Revivo project guidance

Maintain one project-wide Postman collection at `postman/Revivo.postman_collection.json`. When implementing or changing an API endpoint, update that collection and organize requests under the appropriate feature folder. Keep credentials and real secrets out of the collection and its environment files.

## Current handoff

Statistics and expense analytics are implemented on `feature/statistics-expenses`. They use existing odometer and maintenance tables; no migration is needed. The SQL integration test uses a rolled-back fixture. See [PROJECT_HISTORY.md](PROJECT_HISTORY.md) for the calculation rules and verification.

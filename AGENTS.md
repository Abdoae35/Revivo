# Revivo project guidance

Maintain one project-wide Postman collection at `postman/Revivo.postman_collection.json`. When implementing or changing an API endpoint, update that collection and organize requests under the appropriate feature folder. Keep credentials and real secrets out of the collection and its environment files.

## Current handoff

Mileage tracking is implemented on `feature/mileage-notifications`. The additive migration is generated but SQL Server refused the connection, so apply it and run database integration checks when the server is available. See [PROJECT_HISTORY.md](PROJECT_HISTORY.md) for details.

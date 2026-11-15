# Revivo project guidance

Maintain one project-wide Postman collection at `postman/Revivo.postman_collection.json`. When implementing or changing an API endpoint, update that collection and organize requests under the appropriate feature folder. Keep credentials and real secrets out of the collection and its environment files.

## Current handoff

Notification settings and alert aggregation are implemented on `feature/notifications`. The additive migration was applied to the configured SQL Server, and an opt-in integration test verifies settings persistence and ownership with a rolled-back transaction. See [PROJECT_HISTORY.md](PROJECT_HISTORY.md) for details.

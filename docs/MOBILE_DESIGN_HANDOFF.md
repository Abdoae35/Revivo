# Revivo mobile design handoff

## Delivery status — 16 November 2026

New file: [Revivo — Mobile App | Complete UI/UX](https://www.figma.com/design/YpH0vEGvFiVez3qJrFoZIh).

Canvas access became available on resumption. The new file now contains all eight requested pages, 51 feature-screen designs, one Arabic RTL specimen, and 43 dedicated prototype frames. The older design was not read or modified.

The design system includes three variable collections with light/dark theme modes, six Manrope text styles, an elevation style, 27 main components and four variant sets. Screens use editable text, Auto Layout, component instances, vector icons, and chart bars. Only vehicle imagery is raster. Two key screens have dark-theme variants.

Start the connected journey on **08 — Interactive Prototype**, at **Welcome** (`7:2`). [Open prototype](https://www.figma.com/proto/YpH0vEGvFiVez3qJrFoZIh?node-id=7-2&starting-point-node-id=7%3A2). The graph contains 88 verified main-journey links and 193 interactive nodes including navigation. Prototype fields contain scripted example values; they do not accept live input or call APIs.

Figma session ID for resuming this same task: `734e0be8-747d-4d86-8e61-a46cc68ba5f2`. The local workflow ledger is `/tmp/design-system-state-revivo-mobile.json`.

## Navigation and visual direction

Use three primary destinations: Garage, Alerts, and Account. Inside a vehicle, show Overview, History, and Insights as secondary destinations. Keep “Record maintenance” prominent on the vehicle overview and history; show “Update mileage” beside the odometer. Warranties and sharing live within the vehicle context. Shared vehicles have a persistent “Read-only access” label and omit write controls.

The implemented direction uses warm off-white surfaces, graphite typography, deep petrol feature panels, and a restrained citrus accent. Main mobile frames are 390 × 844; the prototype mileage overlay is 390 × 570. Buttons are 56 px high and navigation targets are 64 px high. Manrope is the primary typeface and Noto Sans Arabic is used for the RTL specimen. The product has not been tested with end users.

The Corolla image is an illustrative generated design asset created with Figma AI credits, not an Unsplash photo. Screens label it as a generated design sample. The production attribution requirements below still apply to actual Unsplash images.

## Screen-to-API mapping

`V` below means `/api/vehicles/{vehicleId}`. Paths are the actual mapped routes, with integer route constraints omitted for readability.

| Journey | Existing API | UX constraints |
| --- | --- | --- |
| Register / log in / account | POST `/api/auth/register`, POST `/api/auth/login`, GET `/api/auth/me` | Registration returns an ID, not a JWT. Follow with login before entering protected areas. |
| Garage / vehicle switching | GET `/api/vehicles`, GET `/api/vehicles/shared` | Owned and accepted shared vehicles are separate lists. |
| Add / edit car | GET `/api/car-catalog/makes`, `/models?makeId=...`, `/models/{modelId}/years`; POST `/api/vehicles`; PUT `V` | Make/model selection comes from the catalog. Vehicle DTOs return model IDs, not display names. |
| Vehicle overview | GET `V`, `V/odometer/history`, `V/upcoming-maintenance`, `V/maintenance`, `V/statistics/overview` | Compose several responses. Latest recorded time comes from odometer history; do not invent it when history is empty. |
| Vehicle image | GET `/api/car-catalog/car-image?modelId=...&year=...` | Preserve photographer and Unsplash links and the illustrative-photo disclosure. Support unavailable, unconfigured, rate-limited, and no-image states. |
| Maintenance history / details / record / edit | GET/POST `V/maintenance`; GET/PUT/DELETE `V/maintenance/{id}` | Date, service mileage, decimal EGP cost, category, item, notes. Pagination supported. |
| Recurring schedule / upcoming timeline | PUT `V/maintenance-items/{itemId}/schedule`; GET `V/maintenance-status`, `V/upcoming-maintenance` | Save record first and use its ItemId. Distance and time intervals can coexist. |
| Warranty overview / detail / form | GET `V/warranties`; GET/PUT/DELETE `V/maintenance/{recordId}/warranty` | Warranty belongs to a specific maintenance record; it is separate from the recurring schedule. |
| Odometer update / check-in / history | POST `V/odometer`; GET `V/odometer/history` | Reject decreases. Check-in is a client entry point into the same update flow. No backdated reading input. |
| Expenses / driving insights | GET `V/statistics/overview`, `V/statistics/expenses?year=...`, `V/statistics/mileage?year=...` | Use the returned 12-month arrays, confidence labels, and partial flags. Currency is EGP. |
| Alerts / reminder preferences | GET `V/alerts`; GET/PUT `V/notification-settings` | Preferences are per user and vehicle; updates are owner-only. Delivery is not implemented. |
| Invite / manage access | POST/GET `V/shares`; DELETE `V/shares/{shareId}` | Registered-email invitations, owner-only controls, pending and accepted states. |
| Received invitations | GET `/api/vehicle-invitations`; POST `/api/vehicle-invitations/{shareId}/accept` or `/reject` | Only recipient can respond. Accepted access is read-only; revocation takes effect on subsequent requests. |
| Delete vehicle | DELETE `V` | Existing maintenance blocks deletion; do not promise a one-step cascade. |
| Logout / appearance / language | Client behavior | Clear locally stored credentials on logout. Theme/language preferences are not server-backed. |

## Calculation and save behavior

- Maintenance uses the latest service and configured intervals. Mileage is overdue at zero remaining km; a time deadline is overdue after its date. A missing schedule or baseline returns no next deadline even though the status enum is OnTrack; display “No next service set” rather than implying everything is checked.
- Warranty expiration is based on its start date and duration. It remains within the warning window on its expiration date and becomes expired afterward.
- Mileage analytics interpolate between recorded timestamps in UTC. Never show unavailable months as zero or present a partial period as a complete month. Explain estimates beside charts, with an accessible data list.
- Save the maintenance record first, then the schedule and optional warranty. If a later request fails, show “Record saved; next service was not saved” and retry only the failed step. Do not duplicate the record.
- Deleting a maintenance record with a warranty returns a conflict. Preserve the record and explain the dependency.

## Backend gaps and client responsibilities

- No Firebase delivery, scheduled reminder worker, or invitation email delivery. Mark push delivery as future work; no fake permission-success flow.
- No password-reset, token-refresh, account-edit, account-deletion, or server logout endpoint is mapped. Account information is read-only; token expiry requires signing in again.
- No maintenance item/category search endpoint is mapped. Use free-text category/item fields and clearly local suggestions, rather than implying a searchable server catalog.
- Vehicle responses store only `ImageUrl`, not photographer attribution. A durable attribution solution is needed when displaying a stored photo later; do not attach credits from a different newly fetched image.
- Vehicle DTOs lack make/model display names and an explicit access-level field. The client must resolve catalog names and derive ownership from the authenticated user and owned/shared lists.
- Alerts are calculated vehicle-specific results, with no persisted read/dismiss/snooze state. A garage-wide alerts screen needs client aggregation and must avoid fake unread counts.
- Notification settings can be read by accepted shared users, but only owners can update them. Shared settings should be read-only.
- Theme and language settings are local client preferences. Arabic translation and RTL layouts still require design validation with Arabic text and users.

## Quality review and implementation limitations

- Reviewed rendered welcome, dashboard, dark dashboard, recurring schedule, spending, shared vehicle, foundations, product overview, sparse-mileage example, and Arabic specimen. Corrected an initial content-height clipping issue before composing subsequent screens.
- Audited all eight pages for editable structure, typography, screen inventory, and image placement. Linked main components support the repeated controls. Main journey links were verified from saved Figma reactions after correcting a rolled-back link script.
- Corrected the shared-spending route to prevent access to owner-oriented warranty controls. Shared history and spending have separate read-only destinations. Revocation has a distinct success outcome.
- Checked key text contrast pairs: body 13.45:1, secondary 4.72:1, primary button 12.10:1, dark secondary 8.73:1, warning 5.74:1, error 5.61:1. These checks are not a complete WCAG or assistive-technology audit.
- The Plugin API exposes overlay position as read-only. The mileage overlay is connected with supported centered placement; native Flutter bottom-sheet placement remains an implementation task.
- Feature pages are reference designs; page 08 holds the connected scripted demonstration. The prototype is not a complete simulation of every control, validation rule, or server response.
- Arabic is a layout specimen, pending native-speaker localization review. Dynamic text scaling, screen-reader behavior, platform keyboard handling, and usability with Egyptian drivers must be validated in Flutter and user testing.
- No backend functionality changed. Existing API gaps listed above remain. Repository handoff changes were not committed or pushed in this design task.

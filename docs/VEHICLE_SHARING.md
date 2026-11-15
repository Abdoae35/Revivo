# Vehicle sharing

Vehicle owners can invite another registered Revivo user by email. Invitations are stored in the database; this MVP does not send email. The invited user sees pending invitations in the app and can accept or reject one. An accepted invitation grants read access immediately. Rejection grants no access, and the owner can remove a pending or accepted share.

## Workflow

1. The owner calls `POST /api/vehicles/{vehicleId}/shares` with `{ "email": "invitee@example.com" }`.
2. The invited user calls `GET /api/vehicle-invitations`, then `POST /api/vehicle-invitations/{shareId}/accept` or `/reject`.
3. An accepted user finds the vehicle at `GET /api/vehicles/shared` and can read its detail, maintenance history, odometer history, periodic maintenance status, warranties, alerts, and statistics.
4. The owner calls `GET /api/vehicles/{vehicleId}/shares` to see pending and accepted shares and `DELETE /api/vehicles/{vehicleId}/shares/{shareId}` to cancel an invitation or revoke access.

All routes require a JWT. User identity comes from the token's `sub` claim; request bodies never supply a user ID. A missing vehicle or an inaccessible vehicle returns 404. Invalid email or self-invite returns 400. A duplicate pending or accepted share returns 409. Only the invited user can accept or reject a pending invitation. The owner alone can invite, list or revoke shares.

## Access rules

| Action | Owner | Accepted shared user | Pending or rejected user |
| --- | --- | --- | --- |
| Read vehicle detail, maintenance and odometer history, periodic status, warranties, alerts, statistics | Yes | Yes | No |
| Change vehicle, odometer, maintenance, schedule, warranty, or notification settings | Yes | No | No |
| Invite users, view or revoke vehicle shares | Yes | No | No |

The normal `GET /api/vehicles` list continues to return owned vehicles. `GET /api/vehicles/shared` returns accepted shares only. The access query checks the current share status on each read, so revocation takes effect on the next request. A rejected invitation is kept for history and can be followed by a new invitation. Revoking a pending or accepted share deletes that row and allows a new invitation. The database has a filtered unique index that prevents two pending or accepted shares for the same vehicle and user.

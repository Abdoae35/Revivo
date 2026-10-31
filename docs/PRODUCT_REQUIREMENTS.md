# Revivo — Complete Product & UI/UX Design Brief

You are a Senior Mobile UI/UX Designer working on **Revivo**, a Flutter mobile application for car maintenance and warranty tracking.

## Project Goal

Revivo helps car owners track vehicle maintenance, replacement parts, warranties, expenses, and upcoming services.

The application initially targets the Egyptian market.

This is a real-world portfolio project. Prioritize simplicity, excellent usability, and a premium visual experience. Avoid unnecessary complexity.

## 1. Core Features

### Vehicle Management
- Users can register one or multiple vehicles.
- Enter manufacturer, model, year, optional engine/trim, nickname, and current mileage.
- Support manual vehicle entry when no external API is available.
- Update the odometer at any time.
- Switch between registered vehicles.

### Maintenance Tracking
- Track engine oil, oil filter, air filter, spark plugs, transmission fluid, brake fluid, tires, battery, and custom items.
- Each maintenance item has a customizable mileage interval, time interval, or both.
- Users can select predefined maintenance categories without manually creating everything.
- Do not present generic intervals as manufacturer-approved recommendations.
- Display remaining kilometers and/or days until maintenance.
- Clearly distinguish upcoming, due, and overdue services.

Example: If engine oil was changed at 40,000 km and the interval is 10,000 km, the next service is due at 50,000 km.

### Service History
- Record completed maintenance and repairs.
- Each record includes service type, date, odometer reading, optional cost in EGP, and notes.
- Automatically update the next due maintenance when recording a linked recurring service.
- Display all previous services in a chronological timeline.

### Warranty Tracking
- Track warranties for batteries and other replacement parts.
- Store part name, installation/purchase date, warranty duration, and expiration date.
- Show active, expiring, and expired warranties.
- Warranty expiration is independent of maintenance due dates.
- Allow warranties to be linked to service records.

### Notifications
- Upcoming maintenance reminders.
- Overdue maintenance alerts.
- Expiring warranty reminders.
- Use the latest user-entered mileage; do not assume automatic access to the vehicle's odometer.

### Expenses
- Record service costs.
- Show a simple spending summary.
- Avoid complicated financial analytics in the first version.

## 2. Required Mobile Screens

Design high-fidelity, editable Figma screens for:

1. Splash Screen
2. Onboarding
3. Login
4. Sign Up
5. Add Vehicle
6. Home Dashboard
7. Update Odometer
8. Maintenance Schedule
9. Maintenance Item Details
10. Add/Edit Maintenance Item
11. Record Completed Service
12. Service History
13. Service Record Details
14. Warranty Overview
15. Warranty Details
16. My Garage / Vehicle Details
17. Settings
18. Representative Arabic RTL Screens
19. Empty, Loading, Error, and Success States

## 3. Navigation

Use only three main bottom navigation tabs:

- Home
- Maintenance
- History

Vehicle management, warranties, settings, and forms should be accessible through contextual navigation.

## 4. Visual Design Direction

Use the installed `design-taste-frontend` skill and adapt its relevant principles for mobile UI/UX.

Design style:
- Premium minimal automotive aesthetic
- Warm white backgrounds
- Graphite typography
- Muted teal primary accent
- Amber for upcoming/due maintenance
- Red for overdue maintenance
- Spacious cards and clean layouts
- Consistent 8-point spacing system
- Rounded corners and subtle shadows
- Modern, readable typography
- Light theme initially
- English LTR and Arabic RTL support
- Realistic Egyptian examples
- Kilometers and EGP

Avoid generic AI-generated dashboard aesthetics, unnecessary gradients, excessive decorative elements, and complicated navigation.

## 5. Important User Flows

Design complete flows for:

1. New user registration and adding their first vehicle.
2. Setting up maintenance tracking using editable templates.
3. Updating the current odometer.
4. Recording an oil change and automatically calculating the next service.
5. Checking remaining mileage before spark plug replacement.
6. Recording a battery replacement with a one-year warranty.
7. Viewing upcoming and expired warranties.
8. Reviewing maintenance history and expenses.

## 6. Figma Requirements

- Create all screens in the existing Figma Design file.
- Use editable Figma layers, not flattened screenshots.
- Use Auto Layout and reusable components.
- Establish consistent colors, typography, spacing, and components.
- Organize screens by user flow.
- Create representative Arabic RTL layouts.
- Include realistic sample data.
- Verify visual consistency and readability.
- Make the design practical to implement later in Flutter.

## 7. Execution Instructions

First, save this complete brief inside the Revivo repository as `docs/PRODUCT_REQUIREMENTS.md`.

Then read `.agents/skills/design-taste-frontend/SKILL.md` and apply its relevant design principles.

Use the connected Figma tools to build the complete UI/UX in the existing Figma file.

Work incrementally, starting with the design system and core screens, then completing the remaining flows.

Inspect the actual Figma output and fix any visual issues.

Do not implement Flutter code, backend APIs, or database models yet.

When finished, provide:
- The Figma file link
- A list of completed screens
- Any missing or incomplete screens
- A summary of the design system

Do not ask me to repeat the requirements again. Treat `docs/PRODUCT_REQUIREMENTS.md` as the project's source of truth.

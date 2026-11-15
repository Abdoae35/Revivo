# Database ERD

This diagram reflects `RevivoDbContext` and the latest EF Core model snapshot after migration `20261115145958_AddVehicleShares`. It includes all 11 application tables and 7 ASP.NET Identity tables. The same diagram is available as standalone Mermaid source in [erd.mmd](erd.mmd).

```mermaid
erDiagram
    AspNetUsers {
        guid Id PK
        string Name
        string UserName
        string NormalizedUserName
        string Email
        string NormalizedEmail
        string PasswordHash
        bool EmailConfirmed
    }
    AspNetRoles {
        guid Id PK
        string Name
        string NormalizedName
    }
    AspNetUserClaims {
        int Id PK
        guid UserId FK
        string ClaimType
        string ClaimValue
    }
    AspNetRoleClaims {
        int Id PK
        guid RoleId FK
        string ClaimType
        string ClaimValue
    }
    AspNetUserLogins {
        string LoginProvider PK
        string ProviderKey PK
        guid UserId FK
        string ProviderDisplayName
    }
    AspNetUserRoles {
        guid UserId PK, FK
        guid RoleId PK, FK
    }
    AspNetUserTokens {
        guid UserId PK, FK
        string LoginProvider PK
        string Name PK
        string Value
    }
    CarMakes {
        int Id PK
        string Name
    }
    CarModels {
        int Id PK
        int CarMakeId FK
        string Name
        int YearStart
        int YearEnd
    }
    Vehicles {
        int Id PK
        int CarModelId FK
        guid OwnerId
        int Year
        int CurrentMileageKm
        string Nickname
        string ImageUrl
        datetimeoffset CreatedAt
    }
    MaintenanceCategories {
        int Id PK
        string Name
    }
    MaintenanceItems {
        int Id PK
        int MaintenanceCategoryId FK
        string Name
    }
    MaintenanceRecords {
        int Id PK
        int VehicleId FK
        int MaintenanceItemId FK
        date MaintenanceDate
        int MileageKm
        decimal CostEgp
        string Notes
        datetimeoffset CreatedAt
    }
    MaintenanceSchedules {
        int Id PK
        int VehicleId FK
        int MaintenanceItemId FK
        int IntervalKm
        int IntervalMonths
        int WarningKm
        int WarningDays
    }
    MaintenanceWarranties {
        int MaintenanceRecordId PK, FK
        int DurationMonths
        date StartDate
        int WarningDays
        string Provider
        string Brand
        string Notes
        datetimeoffset CreatedAt
    }
    OdometerReadings {
        int Id PK
        int VehicleId FK
        int MileageKm
        datetimeoffset RecordedAt
        guid RecordedByUserId
    }
    NotificationSettings {
        int Id PK
        int VehicleId FK
        guid UserId
        bool WeeklyReminderEnabled
        int ReminderDayOfWeek
        time ReminderTime
        string TimeZoneId
        bool MaintenanceAlertsEnabled
        bool WarrantyAlertsEnabled
    }
    VehicleShares {
        int Id PK
        int VehicleId FK
        guid InvitedUserId FK
        guid InvitedByUserId FK
        string Status
        datetimeoffset CreatedAt
        datetimeoffset RespondedAt
    }

    AspNetUsers ||--o{ AspNetUserClaims : has
    AspNetUsers ||--o{ AspNetUserLogins : has
    AspNetUsers ||--o{ AspNetUserRoles : has
    AspNetUsers ||--o{ AspNetUserTokens : has
    AspNetRoles ||--o{ AspNetRoleClaims : has
    AspNetRoles ||--o{ AspNetUserRoles : has
    CarMakes ||--o{ CarModels : contains
    CarModels ||--o{ Vehicles : classifies
    MaintenanceCategories ||--o{ MaintenanceItems : contains
    MaintenanceItems ||--o{ MaintenanceRecords : categorizes
    Vehicles ||--o{ MaintenanceRecords : has
    MaintenanceItems ||--o{ MaintenanceSchedules : configures
    Vehicles ||--o{ MaintenanceSchedules : has
    MaintenanceRecords ||--o| MaintenanceWarranties : may_have
    Vehicles ||--o{ OdometerReadings : has
    Vehicles ||--o{ NotificationSettings : has
    Vehicles ||--o{ VehicleShares : shares
    AspNetUsers ||--o{ VehicleShares : invites
    AspNetUsers ||--o{ VehicleShares : receives
```

## Schema notes

- `Vehicles.OwnerId`, `OdometerReadings.RecordedByUserId`, and `NotificationSettings.UserId` are GUID columns used by application logic. The database does **not** define foreign keys from these columns to `AspNetUsers`, so the diagram does not show those links.
- `VehicleShares.InvitedUserId` and `InvitedByUserId` **are** separate foreign keys to `AspNetUsers`. `Status` stores `Pending`, `Accepted`, or `Rejected`. A filtered unique index on `(VehicleId, InvitedUserId)` permits only one pending or accepted share for a vehicle and user.
- `MaintenanceWarranties.MaintenanceRecordId` is both the primary key and the foreign key to `MaintenanceRecords`, making the warranty optional and one-to-one from the record's perspective.
- Unique indexes also enforce make names, model names within a make, maintenance category names, item names within a category, one schedule per vehicle and item, and one notification-settings row per user and vehicle.
- The diagram lists key and representative columns. SQL types, nullability, check constraints, indexes, and delete behavior are defined by the EF Core configurations and migrations in `Revivo.Infrastructure/Persistence`.

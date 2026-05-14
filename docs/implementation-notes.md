# Rehab AI - Implementation Notes

This file records current implementation progress and environment state. It is documentation only and does not change business rules or database schema.

## 1. Current Progress

- The .NET 8 solution structure has been set up for the Rehab AI project.
- The project follows the Clean Architecture / Modular Monolith direction described in `docs/project-decisions.md`.
- Entity Framework Core is configured for SQL Server through the Infrastructure layer.
- The initial EF Core migration has been created.
- The SQL Server database has been created and connected successfully.
- The application currently points to the SQL Server database `RehabAIDb`.
- Infrastructure now includes idempotent EF Core seed logic for core MVP lookup/configuration data.

## 2. Database Setup

- Database engine: SQL Server.
- Database name: `RehabAIDb`.
- EF Core migration created:
  - `20260514050550_InitialCreate`
- Migration folder:
  - `src/RehabAI.Infrastructure\Database\Migrations`
- `dotnet ef database update` has been applied successfully.
- `RehabAIDb` exists in SQL Server and contains the generated tables from the current EF Core model.
- Core seed data has been applied to `RehabAIDb`:
  - Roles: 7 records
  - Specialties: 6 records
  - SubscriptionPlans: 2 records
  - SystemSettings: 4 records

## 3. Recently Completed

- Created and connected the SQL Server database.
- Updated the project connection string to use the main SQL Server instance instead of LocalDB.
- Applied the current EF Core migration successfully.
- Confirmed that `RehabAIDb` exists in SQL Server.
- Confirmed that generated database tables were created by the migration.
- Added Infrastructure-layer database seeding through `DatabaseSeeder`.
- Wired seed execution into API startup through `app.Services.SeedDatabaseAsync()`.
- Seeded core MVP lookup/configuration data:
  - roles
  - specialties
  - subscription plans
  - system settings
- Corrected `src/RehabAI.Api/appsettings.json` to point to the main SQL Server database instead of LocalDB.

## 4. Current Connection

The current connection string is stored in:

```text
src/RehabAI.Api/appsettings.json
```

Current connection string:

```text
Server=localhost;Database=RehabAIDb;Trusted_Connection=True;TrustServerCertificate=True
```

This connection targets the SQL Server instance available through `localhost`.

Seed logic is implemented in:

```text
src/RehabAI.Infrastructure/Database/DatabaseSeeder.cs
```

Seed execution is currently called from:

```text
src/RehabAI.Api/Program.cs
```

## 5. Next Recommended Steps

- Verify seeded records in SQL Server Management Studio by expanding `RehabAIDb` and checking `Roles`, `Specialties`, `SubscriptionPlans`, and `SystemSettings`.
- Begin implementing application services/use cases in small MVP slices:
  - patient registration and email verification
  - admin-created doctor account flow
  - doctor profile and credential metadata
  - medical services and doctor schedule slots
  - appointment booking
  - commerce order/payment flow
- Add tests around critical flows before expanding features:
  - token usage
  - appointment slot reservation
  - payment webhook idempotency
  - account status access rules

## 6. Known Risks

- `docs/database-design.md` still describes the database as a pre-migration design, while the initial migration has now been created and applied. Future documentation updates should keep design and implementation state clearly separated.
- The current database is an initial development database. Future schema changes should be made through new EF Core migrations, not manual SQL edits.
- Seed data is currently inserted at API startup. The seed logic is idempotent, but production deployment should decide whether startup seeding remains acceptable or moves to an explicit deployment/admin initialization step.
- Payment finalization rules depend on verified webhook handling, which still needs implementation.
- Email verification, password reset, and doctor invitation flows depend on token hashing and email delivery services that still need implementation.
- AI chat quota and guest-to-patient session linking are represented in the schema but still need application-layer enforcement.

# Rehab AI

ASP.NET Core .NET 8 modular monolith scaffold for the Rehab AI healthcare platform.

Current SRS baseline: `D:\Rehab_AI_Use_Case_Specification_v6_11_shipping_phases.docx`.

## Projects

```text
src/RehabAI.Api             HTTP API and controllers
src/RehabAI.Application     Use-case services, DTOs, interfaces
src/RehabAI.Domain          Entities, enums, domain model
src/RehabAI.Infrastructure  EF Core, SQL Server, email, payment, AI, storage
tests/                      Unit and integration test projects
docs/                       SRS/use case and design documents
```

## Open In Visual Studio

Open:

```text
D:\RehabAI\RehabAI.sln
```

## Database

The default connection string uses local SQL Server:

```text
Server=localhost;Database=RehabAIDb;Trusted_Connection=True;TrustServerCertificate=True
```

The initial EF Core migration has been created and applied to `RehabAIDb`.

Doctor accounts follow the current Admin-created Doctor flow: Admin creates Doctor accounts internally, sends a single-use password setup invitation, and stores credential document metadata against `DoctorProfile`.

Development Admin test account configuration is stored in:

```text
src/RehabAI.Api/appsettings.Development.json
```

When the API runs in Development, `DatabaseSeeder` reads that section and creates/repairs the configured Admin test account with a hashed password.

# Rehab AI

ASP.NET Core .NET 8 modular monolith scaffold for the Rehab AI healthcare platform.

Current SRS baseline: `docs/Rehab_AI_Use_Case_Specification_v6_9_admin_created_doctors.docx`.

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

The default connection string uses SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=RehabAIDb;Trusted_Connection=True;TrustServerCertificate=True
```

Migrations have not been created yet. Create them after the domain model is confirmed.

Doctor accounts follow the v6.9 flow: Admin creates Doctor accounts internally, sends a single-use password setup invitation, and stores credential document metadata against `DoctorProfile`.

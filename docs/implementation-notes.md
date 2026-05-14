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
- Patient registration MVP has been implemented through a thin API controller, Application-layer use case, and Infrastructure persistence/security adapters.
- Patient email verification MVP has been implemented using hashed token comparison and EF Core persistence updates.
- Patient email verification is testable from Swagger in Development because `register-patient` returns the raw verification token only when the API environment is `Development`.
- Patient email verification token matching has been hardened so token hashing is deterministic and separate from password hashing.
- Login MVP has been implemented for active verified users with password verification and a minimal JWT access token.

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
- Implemented Patient registration MVP:
  - guests can register only through the Patient registration endpoint
  - new users are created with `Status = PendingEmail`
  - the seeded `Patient` role is assigned from the `Roles` table
  - passwords are hashed before storage
  - a `PatientProfile` is created with the user
  - an email verification token is generated
  - only the verification token hash is stored in `UserTokens`
  - the verification token is expiring and single-use through `ExpiresAt` and `UsedAt`
  - an `EmailLog` is created and marked `Sent` by the placeholder email sender path
- Verified the registration endpoint against `RehabAIDb` with a development test user.
- Implemented Patient email verification MVP:
  - verification compares the incoming token hash with stored `UserTokens.TokenHash`
  - token lookup requires `TokenType = EmailVerification`
  - token must belong to the target email/user
  - token must not be expired
  - token must not have `UsedAt` set
  - successful verification sets `UserTokens.UsedAt`
  - successful verification sets `Users.EmailConfirmed = true`
  - successful verification sets `Users.Status = Active`
- Verified the email verification endpoint against `RehabAIDb`:
  - valid token returned success and activated the user
  - reused token returned `409 Conflict`
  - expired token returned `410 Gone`
- Made Patient email verification testable in Development:
  - `POST /api/auth/register-patient` includes `verificationToken` only when `IHostEnvironment.IsDevelopment()` is true
  - `POST /api/auth/register-patient` also includes `swaggerVerifyEmailRequest` in Development for direct Swagger copy/paste testing
  - Production responses do not include the raw token or Swagger helper request
  - `UserTokens` continues storing only `TokenHash`
- Verified both environment behaviors:
  - Development registration response includes the raw token and Swagger helper payload
  - Production registration response does not include token fields
  - SQL verification confirmed `UserTokens.TokenHash` does not store the raw token
- Fixed email verification token matching:
  - `UserTokens.TokenHash` continues to store only deterministic token hashes
  - password hashing remains separate from token hashing
  - registration stores `SecureTokenService.HashToken(rawToken)`
  - verification hashes the incoming token using the same token service
  - token hash comparison now happens through `ISecureTokenService.TokenHashesEqual`
  - Infrastructure token comparison uses `CryptographicOperations.FixedTimeEquals`
  - repository token lookup now scopes by email/user and `EmailVerification` token type before Application compares hashes
- Added unit tests for email verification:
  - valid token verifies successfully
  - reused token maps to `409 Conflict`
  - invalid token maps to `400 Bad Request`
  - expired token maps to `410 Gone`
- Verified the Development Swagger-style flow against `RehabAIDb`:
  - `register-patient` returned a raw development verification token
  - `verify-email` accepted that exact token
  - reusing that token returned `409 Conflict`
  - SQL confirmed the raw token was not stored in `UserTokens.TokenHash`
- Implemented Login MVP:
  - `POST /api/auth/login` accepts email and password
  - only `Active` users can log in normally
  - `PendingEmail` users are blocked with `Please verify your email before logging in.`
  - `PendingPasswordSetup` users are blocked from normal login and directed to the doctor invitation password setup flow
  - locked, suspended, deactivated, and other non-active statuses are blocked
  - passwords are verified using the existing PBKDF2 password hasher
  - password hashes are never exposed in responses
  - successful responses include `userId`, `email`, `fullName`, `roles`, and `accessToken`
  - minimal HMAC-SHA256 JWT token generation was added in Infrastructure
- Added unit tests for Login:
  - active verified Patient login succeeds
  - PendingEmail Patient login returns `403 Forbidden`
  - wrong password returns `401 Unauthorized`
  - unknown email returns `401 Unauthorized`
- Verified the Login endpoint against `RehabAIDb`:
  - verified Active Patient returned `200 OK`
  - PendingEmail Patient returned `403 Forbidden`
  - wrong password returned `401 Unauthorized`
  - unknown email returned `401 Unauthorized`

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

Patient registration implementation files:

```text
src/RehabAI.Api/Controllers/AuthController.cs
src/RehabAI.Api/Contracts/Auth/RegisterPatientRequest.cs
src/RehabAI.Application/Auth/AuthContracts.cs
src/RehabAI.Application/Auth/AuthService.cs
src/RehabAI.Infrastructure/Auth/EfPatientRegistrationRepository.cs
src/RehabAI.Infrastructure/Auth/JwtTokenService.cs
src/RehabAI.Infrastructure/Auth/Pbkdf2PasswordHasher.cs
src/RehabAI.Infrastructure/Auth/SecureTokenService.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Auth/EmailVerificationTests.cs
tests/RehabAI.UnitTests/Auth/LoginTests.cs
```

Patient email verification uses the same Auth implementation files and endpoint:

```text
POST /api/auth/verify-email
```

Development-only Swagger helper behavior:

```text
POST /api/auth/register-patient
```

When the API environment is `Development`, a successful response includes:

```text
verificationToken
swaggerVerifyEmailRequest
```

These fields must not be exposed in Production.

Login endpoint:

```text
POST /api/auth/login
```

JWT settings are stored in:

```text
src/RehabAI.Api/appsettings.json
```

## 5. Next Recommended Steps

- Verify seeded records in SQL Server Management Studio by expanding `RehabAIDb` and checking `Roles`, `Specialties`, `SubscriptionPlans`, and `SystemSettings`.
- Continue implementing application services/use cases in small MVP slices:
  - add focused automated tests for Patient registration validation, duplicate email handling, token storage, and EmailLog status
  - wire JWT bearer authentication middleware and authorization policies for protected endpoints
  - admin-created doctor account flow
  - doctor profile and credential metadata
  - medical services and doctor schedule slots
  - appointment booking
  - commerce order/payment flow
- Add tests around critical flows before expanding features:
  - Patient registration validation, duplicate email handling, token storage, and EmailLog status
  - token usage and email verification
  - appointment slot reservation
  - payment webhook idempotency
  - account status access rules

## 6. Known Risks

- `docs/database-design.md` still describes the database as a pre-migration design, while the initial migration has now been created and applied. Future documentation updates should keep design and implementation state clearly separated.
- The current database is an initial development database. Future schema changes should be made through new EF Core migrations, not manual SQL edits.
- Seed data is currently inserted at API startup. The seed logic is idempotent, but production deployment should decide whether startup seeding remains acceptable or moves to an explicit deployment/admin initialization step.
- Payment finalization rules depend on verified webhook handling, which still needs implementation.
- Password reset and doctor invitation completion flows still need implementation.
- The verification email currently uses the placeholder email sender and includes a raw token in placeholder email content. A real frontend verification URL and production email provider are still needed.
- Development registration responses intentionally expose the raw verification token for Swagger testing only. Production behavior was checked to avoid exposing token helper fields.
- Email verification now has unit coverage for valid, invalid, expired, and reused token paths. Broader integration tests against EF Core should still be added before production hardening.
- JWT generation currently uses minimal local HMAC-SHA256 infrastructure. Production deployments must override the development signing key and should wire full bearer authentication/authorization validation before protecting real endpoints.
- AI chat quota and guest-to-patient session linking are represented in the schema but still need application-layer enforcement.

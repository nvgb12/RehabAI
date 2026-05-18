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
- Admin-created Doctor account MVP has been implemented. Doctors do not self-register in the current implementation.
- Doctor invitation password setup MVP has been implemented.
- Medical Services CRUD MVP has been implemented.
- Doctor Schedule Slots MVP has been implemented according to UC-14 Manage Doctor Schedule.
- Public/Searchable Doctor Listing MVP has been implemented.
- Appointment Booking MVP has been implemented.

## 2. Current Completed MVP Features

- Project/database setup:
  - Rehab AI solution is set up with Clean Architecture / Modular Monolith.
  - SQL Server database `RehabAIDb` is created and connected.
  - EF Core `InitialCreate` migration has been created and applied.
  - Current connection string is stored in `src/RehabAI.Api/appsettings.json`.
  - Database is running on the local SQL Server instance through `localhost`.
- Seed data:
  - Seed logic is implemented in Infrastructure and is idempotent.
  - Roles are seeded, including `Patient`, `Doctor`, `Admin`, `AuthorizedInternalStaff`, `VerificationAdmin`, `SupportStaff`, and `FinanceAdmin`.
  - Specialties are seeded.
  - Subscription plans are seeded.
  - System settings are seeded.
- Patient Registration MVP:
  - Endpoint: `POST /api/Auth/register-patient`.
  - Guests can register only as Patients.
  - New Patient users are created with `Status = PendingEmail`.
  - Passwords are hashed before saving.
  - Patient role is assigned from the seeded `Roles` table.
  - `PatientProfile` is created.
  - Email verification token is generated.
  - Only token hash is stored in `UserTokens`.
  - `EmailLog` is created.
  - Duplicate email returns conflict.
  - Flow has been tested with Swagger and SQL Server Management Studio.
- Patient Email Verification MVP:
  - Endpoint: `POST /api/Auth/verify-email`.
  - Token verification uses deterministic token hashing, not salted password hashing.
  - Valid token sets `Users.EmailConfirmed = true`.
  - Valid token sets `Users.Status = Active`.
  - Valid token sets `UserTokens.UsedAt`.
  - Invalid token returns bad request.
  - Used token returns conflict.
  - Expired token returns gone.
  - In Development only, `register-patient` returns raw verification token/setup helper info for Swagger testing.
  - Production must not expose raw tokens.
  - Flow has been tested with Swagger and SQL Server Management Studio.
- Login MVP:
  - Endpoint: `POST /api/Auth/login`.
  - Login uses email and password.
  - Password is checked with the existing password hasher.
  - Only `Active` users can log in normally.
  - `PendingEmail` users are blocked with a verify-email message.
  - `PendingPasswordSetup` users are blocked from normal login.
  - `Locked`, `Suspended`, and `Deactivated` users are blocked.
  - Successful login returns `userId`, `email`, `fullName`, `roles`, and `accessToken`.
  - Active Patient login has been tested successfully in Swagger.
- Admin-created Doctor Account MVP:
  - Endpoint: `POST /api/admin/doctors`.
  - Doctors do not self-register.
  - Admin creates Doctor accounts.
  - Created Doctor has `Role = Doctor`.
  - Created Doctor has `Status = PendingPasswordSetup`.
  - Created Doctor has `EmailConfirmed = true`.
  - Created Doctor has `PasswordHash = null`.
  - `DoctorProfile` is created.
  - `DoctorInvitation` token is created in `UserTokens`.
  - Raw invitation token is returned only in Development for Swagger testing.
  - `AuditLog` is created for Doctor account creation.
  - Duplicate Doctor email returns conflict.
  - Flow has been tested with Swagger and SQL Server Management Studio.
- Doctor Invitation Password Setup MVP:
  - Endpoint: `POST /api/Auth/setup-doctor-password`.
  - Doctor uses invitation token to set initial password.
  - Token type must be `DoctorInvitation`.
  - Token must match the provided email/user.
  - Token must not be expired.
  - Token must not be used.
  - Successful setup sets `Users.PasswordHash`.
  - Successful setup sets `Users.Status = Active`.
  - Successful setup sets `UserTokens.UsedAt`.
  - Doctor can log in normally after password setup.
  - Successful password setup has been tested in Swagger.
- Medical Services CRUD MVP:
  - Endpoints are implemented for public active service listing/detail and admin create/update/soft-delete.
  - Newly created services default to `IsActive = true` when `isActive` is omitted from the create request.
  - Update keeps respecting the provided `isActive` value.
  - Deleted/inactive services do not appear in public active lists.
  - Flow has been tested with Swagger-style HTTP calls and SQL Server Management Studio.
- Doctor Schedule Slots MVP:
  - Endpoint: `GET /api/doctors/{doctorProfileId}/schedule-slots`.
  - Endpoint: `GET /api/doctors/{doctorProfileId}/available-slots`.
  - Endpoint: `POST /api/doctors/{doctorProfileId}/schedule-slots`.
  - Endpoint: `PUT /api/doctors/{doctorProfileId}/schedule-slots/{slotId}`.
  - Endpoint: `DELETE /api/doctors/{doctorProfileId}/schedule-slots/{slotId}`.
  - New manually created slots default to `Available`.
  - Slot validation requires an existing `DoctorProfile`.
  - Slot validation requires the linked user to have the `Doctor` role and `Active` status.
  - Slot validation requires `StartTime < EndTime`.
  - Slot validation requires `StartTime` to be in the future.
  - Overlapping non-deleted, non-disabled slots for the same DoctorProfile are rejected with conflict.
  - Slots with active appointments cannot be modified or disabled.
  - `DELETE` disables the slot by setting `Status = Disabled`; it does not physically delete the record.
  - Disabled slots do not appear in public available-slot results.
- Public/Searchable Doctor Listing MVP:
  - Endpoint: `GET /api/doctors`.
  - Endpoint: `GET /api/doctors/{doctorProfileId}`.
  - Guests and Patients can browse/search public bookable Doctors.
  - Public listing returns only Doctors whose linked user is `Active`, has the `Doctor` role, has `DoctorProfile.PublicProfileApproved = true`, has a non-deleted profile, and has at least one future `Available` schedule slot.
  - Doctors with `PendingPasswordSetup`, `PendingEmail`, `Locked`, `Suspended`, `Deactivated`, deleted profiles, no future available slot, or only disabled/booked/soft-reserved/past slots are excluded.
  - Optional filters are supported for `keyword`, `specialtyId`, `availableFrom`, and `availableTo`.
  - Public detail returns only if the Doctor is publicly bookable.
- Appointment Booking MVP:
  - Endpoint: `POST /api/appointments`.
  - Endpoint: `GET /api/appointments/{appointmentId}`.
  - Endpoint: `GET /api/patients/{patientProfileId}/appointments`.
  - `POST /api/appointments` accepts the appointment request as a direct JSON body, not wrapped inside a `request` object.
  - `CreateAppointmentRequest` exposes direct GUID fields for `patientProfileId`, `doctorProfileId`, `medicalServiceId`, and `scheduleSlotId`, plus optional `reason`.
  - Active Patients can book appointments with public bookable Doctors.
  - Create request uses `patientProfileId`; the implementation maps it to `Appointments.PatientId = Users.Id` according to the current schema.
  - Booking requires an active Patient account with the `Patient` role.
  - Booking requires a public bookable Doctor profile.
  - Booking requires an active, non-deleted Medical Service.
  - Booking requires a future, non-deleted, `Available` schedule slot belonging to the selected DoctorProfile.
  - Successful booking creates an `Appointment` with `Status = PendingPayment`.
  - Successful booking changes the schedule slot to `SoftReserved`.
  - Successful booking sets both `Appointment.SoftReservedUntil` and `DoctorScheduleSlots.ReservedUntil`.
  - Soft reservation duration uses `SystemSettings.Appointment.SoftReserveMinutes` when available, otherwise defaults to 10 minutes.
  - Appointment creation and slot update run in a database transaction.
  - Booking the same slot again returns `409 Conflict` with `Schedule slot is not available for booking.`
  - Manual Swagger and SQL Server verification confirmed appointment booking works for stroke rehabilitation scenarios such as `Post-stroke rehabilitation consultation`.

## 3. Database Setup

- Database engine: SQL Server.
- Database name: `RehabAIDb`.
- EF Core migration created:
  - `20260514050550_InitialCreate`
- Migration folder:
  - `src/RehabAI.Infrastructure\Database\Migrations`
- `dotnet ef database update` has been applied successfully.
- `RehabAIDb` exists in SQL Server and contains the generated tables from the current EF Core model.
- Core seed data has been applied to `RehabAIDb`:
  - Roles: 7 records (`Patient`, `Doctor`, `Admin`, `AuthorizedInternalStaff`, `VerificationAdmin`, `SupportStaff`, `FinanceAdmin`)
  - Specialties: 6 records
  - SubscriptionPlans: 2 records
  - SystemSettings: 4 records

## 4. Recently Completed

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
- Implemented Admin-created Doctor account MVP:
  - `POST /api/admin/doctors` creates Doctor accounts through a thin Admin controller endpoint
  - new Doctor users are assigned the seeded `Doctor` role
  - new Doctor users are created with `Status = PendingPasswordSetup`
  - new Doctor users are created with `EmailConfirmed = true` because the account is admin-created and uses invitation password setup
  - `PasswordHash` remains `null` until the invitation password setup flow is completed
  - a `DoctorProfile` is created with the selected `SpecialtyId`, optional bio, and the default commission rate from `SystemSettings`
  - a `DoctorInvitation` token is generated, expires after 72 hours, and is single-use through `UsedAt`
  - only the deterministic token hash is stored in `UserTokens`
  - an `EmailLog` is created for the doctor invitation and marked by the placeholder email sender path
  - an `AuditLog` entry is created for doctor account creation
  - Development responses include `invitationToken` and `passwordSetupUrl` for Swagger testing
  - Production responses do not expose the raw invitation token
- Corrected Login status handling so `PendingPasswordSetup` accounts are blocked with `403 Forbidden` before password verification. This keeps admin-created Doctor accounts from normal login until the invitation password setup flow exists.
- Added unit test coverage for `PendingPasswordSetup` login blocking.
- Verified the Admin-created Doctor endpoint against `RehabAIDb`:
  - create Doctor returned `200 OK`
  - duplicate Doctor email returned `409 Conflict`
  - SQL confirmed the created user has the `Doctor` role
  - SQL confirmed the created user has `Status = PendingPasswordSetup`
  - SQL confirmed `DoctorProfile` was created
  - SQL confirmed a `DoctorInvitation` `UserToken` exists
  - SQL confirmed the raw invitation token is not stored in `UserTokens.TokenHash`
  - SQL confirmed an account creation `AuditLog` exists
  - normal login for the pending Doctor returned `403 Forbidden`
- Implemented Doctor invitation password setup MVP:
  - `POST /api/auth/setup-doctor-password` accepts email, invitation token, and initial password
  - setup requires a matching `DoctorInvitation` token for the provided email/user
  - the incoming raw token is hashed with `SecureTokenService.HashToken` before comparison
  - token hash comparison uses the existing secure token comparison path
  - raw invitation tokens are still never stored in `UserTokens`
  - used invitation tokens return `409 Conflict`
  - invalid invitation tokens return `400 Bad Request`
  - expired invitation tokens return `410 Gone`
  - successful setup hashes the password with the existing PBKDF2 password hasher
  - successful setup sets `Users.Status = Active`
  - successful setup sets `UserTokens.UsedAt`
  - Doctors can log in normally after successful password setup
- Added unit tests for Doctor invitation password setup:
  - valid token activates the Doctor account
  - reused token maps to `409 Conflict`
  - invalid token maps to `400 Bad Request`
  - expired token maps to `410 Gone`
- Verified the Doctor invitation password setup endpoint against `RehabAIDb`:
  - valid invitation token returned `200 OK`
  - reused invitation token returned `409 Conflict`
  - invalid invitation token returned `400 Bad Request`
  - expired invitation token returned `410 Gone`
  - SQL confirmed password hash is present after setup
  - SQL confirmed user status changed to `Active`
  - SQL confirmed invitation token is marked used
  - login after password setup returned `200 OK` with the `Doctor` role and access token
- Implemented Medical Services CRUD MVP:
  - public `GET /api/medical-services` returns active, non-deleted medical services only
  - public `GET /api/medical-services/{id}` returns only active, non-deleted services
  - admin `POST /api/admin/medical-services` creates a medical service
  - admin `PUT /api/admin/medical-services/{id}` updates a non-deleted medical service
  - admin `DELETE /api/admin/medical-services/{id}` soft deletes a service by setting `IsDeleted = true` and `IsActive = false`
  - create/update validation requires name, positive duration, non-negative price, and non-negative or null no-show fee amount
  - missing or blank currency defaults to `VND`
  - create defaults `IsActive = true` when `isActive` is omitted
  - update continues to use the provided `isActive` value
  - no appointment booking, doctor scheduling, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Fixed Medical Services create default active behavior:
  - `POST /api/admin/medical-services` now treats omitted `isActive` as active by default
  - `PUT /api/admin/medical-services/{id}` behavior is unchanged and still respects the provided `isActive` value
  - no database schema or migration changes were made
- Added unit tests for Medical Services Application logic:
  - valid create defaults missing currency to `VND`
  - invalid name, duration, price, and no-show fee amount return validation failures
  - update of a missing service returns not found
  - soft delete of an existing service returns success
- Added unit tests for Medical Services API request behavior:
  - create without `isActive` defaults to active
  - update with `isActive = false` remains false
- Verified the Medical Services endpoints against `RehabAIDb`:
  - create medical service returned success
  - public list included the active service before delete
  - public get by id returned the active service
  - update changed service values and defaulted blank currency to `VND`
  - delete returned success
  - public list no longer included the deleted service
  - public get by id after delete returned `404 Not Found`
  - SQL confirmed the deleted service has `IsActive = 0` and `IsDeleted = 1`
- Implemented Doctor Schedule Slots MVP:
  - `GET /api/doctors/{doctorProfileId}/schedule-slots` lists non-deleted slots for a DoctorProfile
  - `GET /api/doctors/{doctorProfileId}/available-slots` lists future `Available` slots only
  - `POST /api/doctors/{doctorProfileId}/schedule-slots` creates a future slot with default `Status = Available`
  - `PUT /api/doctors/{doctorProfileId}/schedule-slots/{slotId}` updates slot time/status
  - `DELETE /api/doctors/{doctorProfileId}/schedule-slots/{slotId}` disables the slot instead of physically deleting it
  - Application-layer validation checks DoctorProfile existence, linked Doctor role, active linked user, valid future time range, overlap conflicts, and active appointment guards
  - No appointment booking, payment, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Doctor Schedule Slots:
  - valid future slot creates an `Available` slot
  - invalid time returns validation failure
  - overlapping slot returns conflict
  - update succeeds for a valid slot
  - disabling a slot with active appointments returns conflict
  - valid disable marks the slot as `Disabled`
  - disabled slot is excluded from available slots
- Verified the Doctor Schedule Slots endpoints against `RehabAIDb` through Swagger-style HTTP calls:
  - create available slot succeeded
  - list doctor's slots succeeded
  - list available slots succeeded
  - overlapping slot returned `409 Conflict`
  - invalid time returned `400 Bad Request`
  - update slot succeeded
  - disable slot succeeded
  - disabled slot did not appear in available slots
- Implemented Public/Searchable Doctor Listing MVP:
  - `GET /api/doctors` returns public bookable Doctor summaries
  - `GET /api/doctors/{doctorProfileId}` returns public Doctor detail only when the Doctor is publicly bookable
  - response includes `doctorProfileId`, `userId`, `fullName`, `specialtyId`, `specialtyName`, `bio`, `avatarUrl`, `nextAvailableSlotStartTime`, and `nextAvailableSlotEndTime`
  - public eligibility requires linked `Users.Status = Active`, Doctor role, `DoctorProfiles.PublicProfileApproved = true`, non-deleted profile, and at least one future `Available` schedule slot
  - `keyword` filter searches Doctor full name and bio
  - `specialtyId` filter restricts results by specialty
  - `availableFrom` and `availableTo` restrict the next available slot window
  - disabled, booked, soft-reserved, deleted, and past slots do not count as available
  - no appointment booking, payment, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Public Doctor Listing Application logic:
  - valid search returns public Doctor summaries
  - invalid availability range returns validation failure
  - empty detail id returns null without repository access
  - valid detail id maps repository result to response
- Verified the Public/Searchable Doctor Listing endpoints against `RehabAIDb` through Swagger-style HTTP calls:
  - active Doctor with `PublicProfileApproved = true` and a future `Available` slot appeared
  - active Doctor with `PublicProfileApproved = false` did not appear
  - Doctor with disabled slot only did not appear
  - `keyword` filter matched the public Doctor
  - `specialtyId` filter matched the public Doctor
  - `availableFrom`/`availableTo` filter matched the public Doctor
  - public Doctor detail returned `200 OK`
  - non-public and non-bookable Doctor detail returned `404 Not Found`
- Implemented Appointment Booking MVP:
  - `POST /api/appointments` creates an appointment from `patientProfileId`, `doctorProfileId`, `medicalServiceId`, and `scheduleSlotId`
  - `POST /api/appointments` now explicitly binds `CreateAppointmentRequest` from the JSON request body through `[FromBody]`
  - `CreateAppointmentRequest` is a direct request DTO with `Guid PatientProfileId`, `Guid DoctorProfileId`, `Guid MedicalServiceId`, `Guid ScheduleSlotId`, and optional `Reason`
  - `GET /api/appointments/{appointmentId}` returns appointment detail
  - `GET /api/patients/{patientProfileId}/appointments` returns appointments for the Patient profile
  - create validates active Patient account and Patient role
  - create validates public bookable Doctor profile
  - create validates active/non-deleted Medical Service
  - create validates the selected slot belongs to the DoctorProfile, is in the future, is not deleted, and is `Available`
  - create prevents double booking by checking active appointments for the selected slot inside the transaction
  - create stores `Appointment.Status = PendingPayment`
  - create stores appointment time snapshots from the selected schedule slot
  - create changes `DoctorScheduleSlots.Status` to `SoftReserved`
  - create sets `DoctorScheduleSlots.ReservedUntil` and `Appointments.SoftReservedUntil`
  - create uses `SystemSettings.Appointment.SoftReserveMinutes`, with a 10 minute fallback
  - no payment capture, gateway, webhook, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Appointment Booking Application logic:
  - valid booking creates a `PendingPayment` appointment
  - inactive Patient is rejected
  - non-public Doctor is rejected
  - unavailable slot returns conflict reason
  - double-booked slot returns conflict reason
  - Patient appointment list returns created appointments
- Verified the Appointment Booking endpoints against `RehabAIDb` through Swagger-style HTTP calls:
  - `POST /api/appointments` created an appointment successfully
  - created appointment status was `PendingPayment`
  - appointment record was persisted in the `Appointments` table
  - related `DoctorScheduleSlots.Status` changed to `SoftReserved` / `Status = 2`
  - `ReservedUntil` was set
  - duplicate booking for the same slot returned `409 Conflict` with `Schedule slot is not available for booking.`
  - `GET /api/appointments/{appointmentId}` returned the created appointment
  - `GET /api/patients/{patientProfileId}/appointments` returned the Patient's appointment list
  - test reason used stroke rehabilitation wording: `Post-stroke rehabilitation consultation`

## 5. Current Connection

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

Admin-created Doctor implementation files:

```text
src/RehabAI.Api/Controllers/AdminController.cs
src/RehabAI.Api/Contracts/Doctors/CreateDoctorRequest.cs
src/RehabAI.Application/Doctors/DoctorContracts.cs
src/RehabAI.Application/Doctors/DoctorService.cs
src/RehabAI.Infrastructure/Doctors/EfDoctorAccountRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Auth/LoginTests.cs
```

Admin-created Doctor endpoint:

```text
POST /api/admin/doctors
```

When the API environment is `Development`, a successful response includes:

```text
invitationToken
passwordSetupUrl
```

These fields must not be exposed in Production.

Doctor invitation password setup endpoint:

```text
POST /api/auth/setup-doctor-password
```

Doctor invitation password setup implementation files:

```text
src/RehabAI.Api/Controllers/AuthController.cs
src/RehabAI.Api/Contracts/Auth/RegisterPatientRequest.cs
src/RehabAI.Application/Auth/AuthContracts.cs
src/RehabAI.Application/Auth/AuthService.cs
src/RehabAI.Infrastructure/Auth/EfPatientRegistrationRepository.cs
tests/RehabAI.UnitTests/Auth/DoctorPasswordSetupTests.cs
```

Medical Services implementation files:

```text
src/RehabAI.Api/Controllers/MedicalServicesController.cs
src/RehabAI.Api/Controllers/AdminController.cs
src/RehabAI.Api/Contracts/MedicalServices/MedicalServiceRequests.cs
src/RehabAI.Application/MedicalServices/MedicalServiceContracts.cs
src/RehabAI.Application/MedicalServices/MedicalServiceManager.cs
src/RehabAI.Infrastructure/MedicalServices/EfMedicalServiceRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/MedicalServices/MedicalServiceManagerTests.cs
```

Medical Services endpoints:

```text
GET /api/medical-services
GET /api/medical-services/{id}
POST /api/admin/medical-services
PUT /api/admin/medical-services/{id}
DELETE /api/admin/medical-services/{id}
```

Doctor Schedule Slots implementation files:

```text
src/RehabAI.Api/Controllers/DoctorsController.cs
src/RehabAI.Api/Contracts/Doctors/ScheduleSlotRequests.cs
src/RehabAI.Application/DoctorSchedules/DoctorScheduleContracts.cs
src/RehabAI.Application/DoctorSchedules/DoctorScheduleSlotService.cs
src/RehabAI.Infrastructure/DoctorSchedules/EfDoctorScheduleSlotRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/DoctorSchedules/DoctorScheduleSlotServiceTests.cs
```

Doctor Schedule Slots endpoints:

```text
GET /api/doctors/{doctorProfileId}/schedule-slots
GET /api/doctors/{doctorProfileId}/available-slots
POST /api/doctors/{doctorProfileId}/schedule-slots
PUT /api/doctors/{doctorProfileId}/schedule-slots/{slotId}
DELETE /api/doctors/{doctorProfileId}/schedule-slots/{slotId}
```

Public/Searchable Doctor Listing implementation files:

```text
src/RehabAI.Api/Controllers/DoctorsController.cs
src/RehabAI.Application/Doctors/PublicDoctorContracts.cs
src/RehabAI.Application/Doctors/PublicDoctorListingService.cs
src/RehabAI.Infrastructure/Doctors/EfPublicDoctorListingRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Doctors/PublicDoctorListingServiceTests.cs
```

Public/Searchable Doctor Listing endpoints:

```text
GET /api/doctors
GET /api/doctors/{doctorProfileId}
```

Supported query filters:

```text
keyword
specialtyId
availableFrom
availableTo
```

Appointment Booking implementation files:

```text
src/RehabAI.Api/Controllers/AppointmentsController.cs
src/RehabAI.Api/Contracts/Appointments/AppointmentRequests.cs
src/RehabAI.Api/Controllers/CoreUseCasesController.cs
src/RehabAI.Application/Appointments/AppointmentContracts.cs
src/RehabAI.Application/Appointments/AppointmentBookingService.cs
src/RehabAI.Infrastructure/Appointments/EfAppointmentBookingRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Appointments/AppointmentBookingServiceTests.cs
```

Appointment Booking endpoints:

```text
POST /api/appointments
GET /api/appointments/{appointmentId}
GET /api/patients/{patientProfileId}/appointments
```

Direct appointment booking request body:

```json
{
  "patientProfileId": "guid",
  "doctorProfileId": "guid",
  "medicalServiceId": "guid",
  "scheduleSlotId": "guid",
  "reason": "Post-stroke rehabilitation consultation"
}
```

## 6. Git/Branch State

- Current working branch: `Test`.
- Recent completed features should be committed and pushed to `origin/Test`.
- The working tree currently contains uncommitted feature/documentation changes.
- `main` will not show the latest changes until branch `Test` is merged into `main` through a Pull Request.

## 7. Next Recommended Steps

1. Commit/push any uncommitted changes to `origin/Test`.
2. Medical Services CRUD MVP is implemented locally in this workspace; make sure it is committed/pushed with the current feature set. If working from an older checkpoint without these local changes, implement Medical Services CRUD MVP before schedule work.
3. Doctor Schedule Slots MVP is implemented locally in this workspace; make sure it is committed/pushed with the current feature set.
4. Public/Searchable Doctor Listing MVP is implemented locally in this workspace; make sure it is committed/pushed with the current feature set.
5. Appointment Booking MVP is implemented locally in this workspace; make sure it is committed/pushed with the current feature set.
6. Implement appointment payment initialization and verified payment webhook handling.
7. AI/subscription quota is handled by another team and should not be implemented unless assigned.

## 8. Known Risks

- `docs/database-design.md` still describes the database as a pre-migration design, while the initial migration has now been created and applied. Future documentation updates should keep design and implementation state clearly separated.
- The current database is an initial development database. Future schema changes should be made through new EF Core migrations, not manual SQL edits.
- Seed data is currently inserted at API startup. The seed logic is idempotent, but production deployment should decide whether startup seeding remains acceptable or moves to an explicit deployment/admin initialization step.
- Payment finalization rules depend on verified webhook handling, which still needs implementation.
- Password reset still needs implementation.
- The verification email currently uses the placeholder email sender and includes a raw token in placeholder email content. A real frontend verification URL and production email provider are still needed.
- Development registration responses intentionally expose the raw verification token for Swagger testing only. Production behavior was checked to avoid exposing token helper fields.
- Development Doctor creation responses intentionally expose the raw invitation token for Swagger testing only. Production behavior must continue to avoid exposing invitation token helper fields.
- `POST /api/admin/doctors` is implemented but not yet protected by an Admin-only authorization policy because bearer authentication/authorization enforcement is still a future slice.
- Medical Services admin endpoints are implemented but not yet protected by an Admin-only authorization policy because bearer authentication/authorization enforcement is still a future slice.
- Doctor Schedule Slots endpoints are implemented but not yet protected by Doctor/authorized Staff authorization policies because bearer authentication/authorization enforcement is still a future slice.
- Doctor Schedule Slots currently guard against active appointments during update/disable, but appointment booking/rescheduling workflows are not implemented yet.
- Public Doctor listing is intentionally unauthenticated for Guests and Patients, but Admin-only profile approval management is not implemented yet.
- Appointment Booking now moves slots to `SoftReserved`; payment webhook handling still needs to move successful appointments to `Pending` or `Confirmed` and slots to `Booked`.
- Pending payment expiration is not implemented yet; expired appointments still need a background job or command to return slots to `Available` and clear `ReservedUntil`.
- Appointment Booking endpoints are implemented but not yet protected by authenticated Patient authorization policy because bearer authentication/authorization enforcement is still a future slice.
- Appointment Booking request IDs must be valid GUID strings. Invalid GUID input, such as a copied `scheduleSlotId` with a missing character, is rejected by ASP.NET Core model binding before appointment business rules run.
- `CreateDoctorRequest.YearsOfExperience` is accepted for API compatibility with the current request shape, but it is not persisted because the current `DoctorProfile` schema does not include a `YearsOfExperience` column and this task did not change schema or create a migration.
- Doctor invitation password setup is implemented, but the real frontend setup page and production email URL are still needed.
- Email verification now has unit coverage for valid, invalid, expired, and reused token paths. Broader integration tests against EF Core should still be added before production hardening.
- JWT generation currently uses minimal local HMAC-SHA256 infrastructure. Production deployments must override the development signing key and should wire full bearer authentication/authorization validation before protecting real endpoints.
- AI chat quota and guest-to-patient session linking are represented in the schema but still need application-layer enforcement.

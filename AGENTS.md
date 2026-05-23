# Rehab AI - Codex Working Instructions

## Project Summary

Rehab AI is a healthcare and rehabilitation web platform.

The system supports:
- Patient registration and email verification.
- Admin-created Doctor accounts.
- Doctor schedules and appointment booking.
- AI chatbot with Guest and Patient access.
- AI quota, subscription gating, and prompt safety.
- Product store, cart, orders, payments, and refunds.
- Admin management, audit logging, and email notifications.

## Current Architecture

Use ASP.NET Core Web API with Clean Architecture / Modular Monolith.

Solution structure:

- `RehabAI.Domain`
- `RehabAI.Application`
- `RehabAI.Infrastructure`
- `RehabAI.API`

Do not put business logic in controllers.

Controllers should only:
- receive requests
- validate request shape if needed
- call application services or handlers
- return responses

Business rules should be implemented in the Application layer and Domain layer.

Infrastructure details such as EF Core, email, payment, storage, and AI provider integrations belong in the Infrastructure layer.

## Source of Truth

Before coding, read these files:

1. `/docs/project-decisions.md`
2. `/docs/database-design.md`
3. `/docs/SRS-RehabAI.md`
4. `/docs/implementation-notes.md` if it exists

If there is a conflict between documents, follow this priority:

1. `/docs/project-decisions.md`
2. `/docs/database-design.md`
3. `/docs/SRS-RehabAI.md`
4. Existing code

If the conflict blocks implementation, ask for clarification before changing business rules.

## Key Business Decisions

### Patient Registration

Guests can register only as Patients.

Patient account flow:

1. Guest registers with full name, email, phone number, and password.
2. System creates a Patient user with `Status = PendingEmail`.
3. System sends an email verification token.
4. User verifies email.
5. System changes user status to `Active`.
6. Patient can log in and use protected Patient features.

### Doctor Onboarding

Doctors cannot self-register.

Doctor accounts are created internally by Admin or explicitly delegated authorized staff.

Doctor account flow:

1. Admin creates Doctor user.
2. System creates Doctor profile.
3. User has Doctor role.
4. User status starts as `PendingPasswordSetup`.
5. System sends Doctor invitation email with a single-use token.
6. Doctor sets initial password using the invitation token.
7. User status becomes `Active` after successful password setup and policy checks.

Pending Doctor users cannot access the normal platform.

Users with `Status = PendingPasswordSetup` may only access the password setup invitation flow.

### Doctor Credential Documents

Doctor credential/license documents are sensitive.

Rules:
- Store credential/license files in private storage only.
- Database stores metadata only.
- Never store permanent public URLs for credential documents.
- Only authorized Admin or delegated verification roles can view credential documents.
- All credential document access must be audit logged.

### Public Doctor Visibility

Doctor `Active` status alone is not enough for public visibility.

A Doctor can appear in public Search, public Doctor detail, or AI suggestions only when all conditions are true:

- `Users.Status = Active`
- `DoctorProfiles.PublicProfileReviewStatus = Approved`
- `DoctorProfiles.PublicProfileApproved = true`
- `DoctorProfiles.IsDeleted = false`

Future available schedule slots are not required for public visibility.

Schedule slots are bookability metadata only. They determine direct slot booking availability and next-slot display, but lack of future available slots must not hide an otherwise Active and Approved public Doctor.

## Account Access Rules

Only users with `Status = Active` can access normal platform features.

Exceptions:
- `PendingEmail` users may access email verification flow.
- `PendingPasswordSetup` users may access initial password setup flow.
- Password reset requires valid account and valid token rules.
- Suspended, Locked, Deactivated users cannot access normal platform features.

## Database Rules

Use SQL Server and Entity Framework Core.

General rules:
- Use `Guid` / `uniqueidentifier` for primary keys.
- Use `DateTimeOffset` / `datetimeoffset` for timestamps.
- Use soft delete for business-critical records.
- Use enum types for statuses.
- Store token hashes only; never store raw tokens.
- Use Fluent API configurations for relationships, indexes, constraints, and precision.

Important database files:
- `/docs/database-design.md` is the source of truth for entities and migrations.

## Appointment Rules

Appointment state machine:

- `Requested -> PendingPayment`
- `Requested -> Rejected`
- `PendingPayment -> Expired`
- `PendingPayment -> Pending`
- `Pending -> Confirmed`
- `Pending -> Cancelled`
- `Confirmed -> Completed`
- `Confirmed -> Cancelled`
- `Confirmed -> NoShow`

When a paid appointment is created:
- Appointment status starts as `PendingPayment`.
- The schedule slot becomes `SoftReserved`.
- `SoftReservedUntil` / `ReservedUntil` is set based on system settings.

When payment succeeds through verified webhook:
- Payment becomes `Paid`.
- Appointment moves from `PendingPayment` to `Pending` or `Confirmed` depending on service auto-confirm rule.
- Schedule slot becomes `Booked`.

When payment times out:
- Appointment becomes `Expired`.
- Doctor schedule slot returns to `Available`.
- `ReservedUntil` is cleared.

Flexible appointment request rules:

- A Patient can send an appointment request to an Active and Approved Doctor without selecting a schedule slot.
- Flexible request creation sets `AppointmentStatus = Requested`.
- `DoctorScheduleSlotId` may be null for flexible appointment requests.
- Doctor acceptance moves the appointment from `Requested` to `PendingPayment`.
- Doctor rejection moves the appointment from `Requested` to `Rejected` and requires a rejection reason.
- Flexible requests do not reserve or book schedule slots in the current MVP.

## Payment Rules

Payment finalization must happen through verified webhook, not redirect alone.

Use `PaymentWebhookEvents` to prevent duplicate webhook processing.

Payments use one shared table.

Each payment must point to exactly one target:
- Order
- Appointment
- Subscription

No-show fee payments use `AppointmentId`.

Never store:
- raw card data
- payment secret keys
- unredacted sensitive webhook data

## AI Chat Rules

Guest users can use limited AI chat.

Patient users can access authenticated AI chat based on subscription plan and quota.

Guest chat sessions may be linked to a Patient account after successful login.

Rules:
- Track daily usage in `AiUsageDaily`.
- Enforce quota server-side.
- Sanitize AI input to reduce prompt injection risk.
- Do not expose private patient records to the AI unless explicitly allowed by future requirements.
- Do not let AI directly write booking records. AI can guide or pre-fill flows only.

## Subscription Rules

Subscription plans are stored in `SubscriptionPlans`.

A user may have only one current subscription in one of these current states:
- Active
- PastDue
- Cancelled until period end

Pro AI features must be checked server-side.

Do not trust client-side subscription status.

## Audit Rules

Audit logs are required for sensitive actions:

- Admin creates Doctor account
- Admin updates roles or account status
- Credential document upload
- Credential document access
- Appointment status changes
- Payment and refund actions
- Subscription changes
- Admin management actions

Audit logs should be immutable and should not be soft-deleted.

## Security Rules

Enforce RBAC server-side.

Do not rely on frontend checks for authorization.

Sensitive document access must require authorization.

Token rules:
- Store only token hashes.
- Tokens must be single-use.
- Tokens must expire.
- Used or expired tokens must be rejected.

## Coding Style

Use names that match `/docs/database-design.md`.

Use enum types for:
- Account status
- User token type
- Appointment status
- Schedule slot status
- Payment status
- Payment purpose
- Subscription status
- Email status
- Webhook processing status

Use EF Core Fluent API configurations.

Recommended project organization:

```text
src/
  RehabAI.Domain/
    Entities/
    Enums/
    Common/

  RehabAI.Application/
    Features/
    DTOs/
    Common/
    Interfaces/

  RehabAI.Infrastructure/
    Persistence/
    Configurations/
    Services/

  RehabAI.API/
    Controllers/
    Middlewares/
    Extensions/
    Authorization/
    Contracts/

tests/
  RehabAI.UnitTests/
  RehabAI.IntegrationTests/
```

## Implementation Rules

Before making changes:

1. Read the relevant docs.
2. Identify affected modules.
3. Implement the smallest correct change.
4. Do not invent new business rules unless asked.
5. After each task, update `/docs/implementation-notes.md`.
6. If database schema changes, update `/docs/database-design.md`.
7. If business rules change, update `/docs/project-decisions.md`.
8. If Codex working rules change, update `/AGENTS.md`.
9. Keep the SRS, database design, project decisions, implementation notes, Codex working rules, and code consistent.

## Testing Rules

When implementing business logic:

- Add or update unit tests when possible.
- Add integration tests for persistence, authentication, payment webhook, and appointment booking flows when possible.
- Do not skip validation for critical flows such as payment, booking, doctor account creation, and token usage.

## Migration Rules

Before creating or changing EF Core migrations:

1. Read `/docs/database-design.md`.
2. Confirm whether the affected feature is MVP or Phase 2.
3. Use Fluent API for:
   - relationships
   - indexes
   - unique constraints
   - check constraints
   - decimal precision
   - delete behavior
4. Do not create migrations for Phase 2 tables unless explicitly requested.

## Phase 2 Features

Do not implement these unless explicitly requested:

- Disputes
- Reviews
- Payouts
- PayoutItems
- Advanced analytics
- Mobile app
- Multi-tenant hospital management
- Real-time video consultation

The database design may mention Phase 2 features, but MVP implementation should focus on the confirmed MVP scope first.

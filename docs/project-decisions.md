# Rehab AI - Project Decisions

This file records decisions that override older assumptions in the SRS or previous conversations.

## 1. Product Direction

Rehab AI is a healthcare and rehabilitation platform.

The current target is a web application that can grow into a real product, not only a demo.

The project should use:

- ASP.NET Core Web API
- Clean Architecture
- Modular Monolith style
- Entity Framework Core
- SQL Server

## 2. Architecture Decision

Use Clean Architecture with four main projects:

```text
src/
  RehabAI.Domain
  RehabAI.Application
  RehabAI.Infrastructure
  RehabAI.API
```

Layer rules:

- Domain contains entities, enums, value objects, and core business logic.
- Application contains use cases, DTOs, interfaces, commands, queries, and validation.
- Infrastructure contains EF Core, email, payment, AI provider, storage, and background jobs.
- API contains controllers, middleware, authentication configuration, and endpoint wiring.

Controllers must stay thin.

Business logic should not be placed in controllers.

## 3. Patient Registration Decision

Guests can register only as Patients.

Patient registration flow:

1. Guest enters full name, email, phone number, and password.
2. System creates a Patient user.
3. User status is `PendingEmail`.
4. System sends email verification token.
5. User clicks verification link.
6. System validates token.
7. User status becomes `Active`.
8. Patient can log in.

Patient email verification token must be:
- single-use
- time-limited
- stored as hash only

## 4. Doctor Onboarding Decision

Doctors do not self-register.

Doctor accounts are created by Admin or explicitly delegated authorized staff.

Doctor onboarding flow:

1. Admin creates Doctor account.
2. System creates User with Doctor role.
3. User status is `PendingPasswordSetup`.
4. System creates or prepares DoctorProfile.
5. Admin may upload or record credential/license documents.
6. System sends Doctor invitation email.
7. Doctor opens invitation link.
8. Doctor sets initial password.
9. System marks invitation token as used.
10. User status becomes `Active` after successful setup and policy checks.

Doctors in `PendingPasswordSetup` cannot access normal platform features.

They can only access the password setup invitation page.

## 5. Removed Doctor Self-Application

The old Doctor self-application flow is removed.

There is no public Doctor registration form in the current scope.

The following concept should not be reintroduced unless explicitly requested:

```text
Guest selects Doctor registration
Doctor uploads certificate by themselves
Doctor waits for Admin approval
Doctor account is approved/rejected as an application
```

The old `DoctorApplications` table is removed from the MVP database design.

Credential documents are linked directly to `DoctorProfiles`.

## 6. Doctor Credential Documents

Doctor credential/license files are sensitive.

Rules:

- Store actual files in private storage.
- Database stores metadata only.
- Do not store permanent public file URLs.
- Use private storage key, not public URL.
- Only authorized Admin or delegated verification roles may access documents.
- All access must be audit logged.
- Malware scan status should be tracked.

If the organization stores credentials outside the platform, use:

```text
DoctorCredentialDocuments.StorageSkippedByPolicy = true
```

## 7. Account Status

Use account status for lifecycle, not permissions.

Roles and account status are separate.

Recommended account statuses:

```text
PendingEmail
PendingPasswordSetup
Active
Locked
Suspended
Deactivated
```

Rules:

- `PendingEmail`: user must verify email.
- `PendingPasswordSetup`: admin-created Doctor must set initial password.
- `Active`: user can access normal platform features.
- `Locked`: account is locked due to security or policy.
- `Suspended`: account is temporarily blocked.
- `Deactivated`: account is disabled.

Only `Active` users can access normal platform features.

## 8. Roles

Roles are stored in relational tables:

```text
Roles
UserRoles
```

This allows flexible permission grouping.

Possible roles:

```text
Patient
Doctor
Admin
AuthorizedInternalStaff
VerificationAdmin
SupportStaff
FinanceAdmin
```

Role does not replace account status.

A Doctor role with `PendingPasswordSetup` is still not allowed to use normal Doctor features.

## 9. Public Doctor Visibility

Doctor `Active` account status does not automatically mean public visibility.

Doctor appears in public search and AI doctor suggestions only if:

```text
Users.Status = Active
DoctorProfiles.PublicProfileApproved = true
At least one DoctorScheduleSlot.Status = Available
DoctorScheduleSlot.StartTime > current time
```

If a Doctor has no future available slot, they should not appear as bookable.

## 10. Schedule And Appointment Booking

Doctor schedule uses explicit slots.

`DoctorScheduleSlots.Status` is the source of slot availability.

Recommended statuses:

```text
Available
SoftReserved
Booked
Disabled
```

Appointment state machine:

```text
PendingPayment -> Expired
PendingPayment -> Pending
Pending -> Confirmed
Pending -> Cancelled
Confirmed -> Completed
Confirmed -> Cancelled
Confirmed -> NoShow
```

When a paid appointment is waiting for payment:

- Appointment status is `PendingPayment`.
- Schedule slot status is `SoftReserved`.
- `ReservedUntil` is set.

When payment succeeds:

- Payment is marked `Paid`.
- Appointment moves to `Pending` or `Confirmed`.
- Slot becomes `Booked`.

When payment times out:

- Appointment becomes `Expired`.
- Slot returns to `Available`.
- `ReservedUntil` is cleared.

## 11. Payment Decision

Use one shared `Payments` table.

A payment must point to exactly one target:

```text
Order
Appointment
Subscription
```

No-show fee payments use `AppointmentId`.

Payment finalization must happen through verified webhook.

Redirect/callback alone must not finalize payment.

Use `PaymentWebhookEvents` to make webhook handling idempotent and prevent duplicate processing.

Payment provider may be:

```text
Stripe
VNPay
MoMo
```

Do not store raw card data.

Do not expose payment secret keys.

## 12. Product Store Decision

Product store and orders are included in MVP because the SRS includes store/order flows and `Payments.OrderId` requires order tables to exist.

MVP commerce tables:

```text
ProductCategories
Products
Carts
CartItems
Orders
OrderItems
```

Orders and order items should store snapshots such as product name, unit price, and subtotal.

## 13. Subscription Decision

Subscription supports Free and Pro plans.

Plans are stored in `SubscriptionPlans`.

Subscriptions use `PlanId` and `PlanCodeSnapshot`.

A user may have only one current subscription in current states such as:

```text
Active
PastDue
Cancelled until period end
```

AI Pro features must be checked server-side.

Do not trust client-side subscription status.

## 14. AI Chat Decision

Guest users may chat with limited quota.

Patient users may chat based on subscription tier.

AI usage is tracked daily in `AiUsageDaily`.

Guest session can be linked to Patient account after login.

Chat rules:

- Sanitize input to reduce prompt injection risk.
- Enforce quota server-side.
- Do not allow AI to access private patient data unless explicitly implemented later.
- AI should not directly create bookings.
- AI may guide user or pre-fill booking flow.

## 15. Audit Decision

Audit logs are required and should not be soft-deleted.

Audit these actions:

- Doctor account creation
- Doctor credential document upload
- Doctor credential document access
- Role changes
- Account status changes
- Appointment status changes
- Payment and refund actions
- Subscription changes
- Admin management actions

## 16. Database Decision

The current database source of truth is:

```text
/docs/database-design.md
```

The database design is ready for MVP entity creation and initial migration.

Current MVP tables:

```text
Users
Roles
UserRoles
UserTokens
PatientProfiles
DoctorCredentialDocuments
DoctorProfiles
Specialties
MedicalServices
DoctorServices
DoctorScheduleSlots
Appointments
AppointmentStatusHistories
ProductCategories
Products
Carts
CartItems
Orders
OrderItems
Payments
PaymentWebhookEvents
Refunds
SubscriptionPlans
Subscriptions
ChatSessions
ChatMessages
AiUsageDaily
EmailLogs
AuditLogs
SystemSettings
```

Phase 2 tables:

```text
Disputes
Reviews
Payouts
PayoutItems
```

Do not implement phase 2 unless explicitly requested.

## 17. MVP Scope

MVP should prioritize:

1. Project setup and Clean Architecture structure
2. Identity and roles
3. Patient registration and email verification
4. Admin-created Doctor accounts
5. Doctor profile and credential metadata
6. Medical services
7. Doctor schedules
8. Appointment booking
9. Product store and orders
10. Payments and webhook handling
11. Subscription plans
12. AI chat quota tracking
13. Email logs
14. Audit logs
15. System settings

## 18. Not In MVP Unless Requested

Do not implement these in the first pass unless explicitly requested:

- Disputes
- Reviews
- Payouts
- PayoutItems
- Advanced analytics
- Mobile app
- Multi-tenant hospital management
- Real-time video consultation

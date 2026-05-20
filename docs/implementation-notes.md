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
- Payment Confirmation Placeholder MVP has been implemented for appointment web-flow testing without a real payment gateway.
- Appointment Cancellation MVP has been implemented for cancelling booked or payment-pending appointments.
- Patient Profile Management MVP has been implemented for viewing and updating existing Patient profile fields.
- Admin Product Management MVP has been implemented for hospital/platform-owned stroke rehabilitation products.
- Public Product Listing MVP has been implemented for Guest/Patient browsing of active in-stock stroke rehabilitation products.
- Order Creation MVP has been implemented for Patient product orders with stock-limit validation and PendingPayment status.
- Product Payment Confirmation Placeholder MVP has been implemented for local product order payment-flow testing.
- Admin Order Management MVP has been implemented for Admin viewing and status management of hospital/platform-owned product orders.
- Admin Order Status validation and Swagger documentation have been improved so testers can see the allowed status values.
- Revenue Report MVP has been implemented for Admin viewing product order revenue and appointment/service revenue statistics.
- Patient Purchase History MVP has been implemented for authenticated Active Patient users viewing their own product orders.
- Subscription Purchase Placeholder MVP has been implemented for public plan listing and authenticated Active Patient subscription purchase/payment-flow testing.
- Authentication and authorization have been audited and hardened with DB-backed active-status/role policies before frontend development.
- React frontend foundation has been created under `frontend/` using Vite, TypeScript, Tailwind CSS, React Router, Axios, TanStack Query, React Hook Form, Zod, Lucide React, and Recharts.

## 2. Current Completed MVP Features

- Project/database setup:
  - Rehab AI solution is set up with Clean Architecture / Modular Monolith.
  - SQL Server database `RehabAIDb` is created and connected.
  - EF Core `InitialCreate` migration has been created and applied.
  - Current connection string is stored in `src/RehabAI.Api/appsettings.json`.
  - Database is running on the local SQL Server instance through `localhost`.
- Frontend foundation:
  - Vite React TypeScript app is created in `frontend/`.
  - Tailwind CSS is configured for the RehabAI healthcare UI foundation.
  - React Router is configured with public routes and protected Patient/Admin routes.
  - Axios API client reads `VITE_API_BASE_URL` and defaults to `https://localhost:7007`.
  - Axios attaches the JWT access token from localStorage for MVP protected requests.
  - Initial pages are created for Home, Login, Register, Products, Doctors, Patient Dashboard, and Admin Dashboard.
  - Initial shared components are created for header, footer, search, product card, doctor card, stat card, loading, error, and empty states.
  - `npm install`, `npm run build`, and `npm run lint` pass in `frontend/`.
- Seed data:
  - Seed logic is implemented in Infrastructure and is idempotent.
  - Roles are seeded, including `Patient`, `Doctor`, `Admin`, `AuthorizedInternalStaff`, `VerificationAdmin`, `SupportStaff`, and `FinanceAdmin`.
  - Specialties are seeded.
  - Subscription plans are seeded.
  - System settings are seeded.
- Authentication/Authorization Hardening:
  - JWT bearer authentication is wired in API startup.
  - Protected endpoint policies now validate the authenticated user against the database on each request.
  - Protected policies require `Users.Status = Active`.
  - `PendingEmail`, `PendingPasswordSetup`, `Locked`, `Suspended`, and `Deactivated` users are blocked from normal protected endpoints even if they have a token.
  - `ActivePatient` policy is used for Patient-only web-flow endpoints.
  - `ActiveAdmin` policy is used for Admin management/reporting endpoints.
  - `ActiveDoctorStaffOrAdmin` policy is used for Doctor schedule and credential scaffold endpoints.
  - Patient profile routes verify the route `patientProfileId` belongs to the current authenticated Patient before returning or updating profile data.
  - Appointment routes verify the appointment or patient profile belongs to the current authenticated Patient before returning, confirming payment placeholder, cancelling, or listing appointment data.
  - Product order routes verify route/body patient/order identifiers belong to the current authenticated Patient before creating, returning, confirming payment placeholder, or listing order data.
  - Subscription routes use the authenticated current-user claim and do not accept patient identifiers from client input.
  - Doctor schedule routes allow Admin/authorized internal staff access, while Doctor users can manage only their own DoctorProfile.
  - Swagger/OpenAPI now applies Bearer JWT security metadata only to operations that have `[Authorize]`, so public endpoints remain visibly public in Swagger.
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
- Patient Profile Management MVP:
  - Endpoint: `GET /api/patients/{patientProfileId}/profile`.
  - Endpoint: `PUT /api/patients/{patientProfileId}/profile`.
  - Uses the existing `PatientProfiles` table/model.
  - Profile detail returns safe profile/account fields only: `patientProfileId`, `userId`, `fullName`, `email`, `phoneNumber`, `dateOfBirth`, `gender`, and `address`.
  - Profile detail does not expose `PasswordHash` or sensitive authentication data.
  - Update supports existing schema fields: `dateOfBirth`, `gender`, and `address`.
  - Missing or deleted Patient profiles return `404 Not Found`.
  - Stroke-specific rehabilitation notes or condition notes are not in the current schema and should be added through a future migration if assigned.
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
- Admin Product Management MVP:
  - Endpoint: `GET /api/admin/products`.
  - Endpoint: `GET /api/admin/products/{productId}`.
  - Endpoint: `POST /api/admin/products`.
  - Endpoint: `PUT /api/admin/products/{productId}`.
  - Endpoint: `DELETE /api/admin/products/{productId}`.
  - Uses the existing `Products` and `ProductCategories` schema.
  - Products are hospital/platform-owned stroke rehabilitation products, not doctor-owned marketplace items.
  - Admin list includes active and inactive products but excludes soft-deleted products.
  - Product create/update supports `name`, `description`, `categoryId`, `price`, `currency`, `stockQuantity`, `imageUrl`, and `isActive`.
  - Product create defaults `currency` to `VND` when omitted.
  - Product create defaults `IsActive = true` when `isActive` is omitted.
  - Product price must be greater than or equal to 0.
  - Product stock quantity must be greater than or equal to 0.
  - Product category must already exist and must not be deleted.
  - Duplicate generated product slugs are rejected with conflict.
  - `DELETE` soft-deletes the product and also marks it inactive; it does not physically delete the record.
  - Cart, order, shipping, payment gateway, AI chat, subscription quota, and Phase 2 behavior were not added in this slice.
- Public Product Listing MVP:
  - Endpoint: `GET /api/products`.
  - Endpoint: `GET /api/products/{productId}`.
  - Guests and Patients can browse hospital/platform-owned healthcare and stroke rehabilitation products.
  - Public listing returns only products that are active, not deleted, and have `StockQuantity > 0`.
  - Public detail returns `404 Not Found` for inactive, deleted, or out-of-stock products.
  - Optional filters are supported for `keyword` and `categoryId`.
  - Public responses return `productId`, `categoryId`, `categoryName`, `name`, `slug`, `description`, `price`, `currency`, `stockQuantity`, and `imageUrl`.
  - Public responses do not expose admin-only fields such as `isActive`.
  - Cart, order, payment gateway, AI chat, subscription quota, and Phase 2 behavior were not added in this slice.
- Order Creation MVP:
  - Endpoint: `POST /api/orders`.
  - Endpoint: `GET /api/orders/{orderId}`.
  - Endpoint: `GET /api/patients/{patientProfileId}/orders`.
  - Uses the existing `Orders` and `OrderItems` schema.
  - Request body includes `patientProfileId`, `items` with `productId` and `quantity`, and `shippingAddress`.
  - `patientProfileId` must exist and must not be deleted.
  - Order must contain at least one item.
  - Product must exist, be active, and not be deleted.
  - Quantity must be greater than 0.
  - Quantity must not exceed current `Products.StockQuantity`.
  - Duplicate product ids in one request are aggregated before stock validation and order item creation.
  - Successful creation stores `Orders.Status = PendingPayment`.
  - Successful creation stores `Orders.PaymentStatus = Pending`.
  - Successful creation snapshots current product name and unit price into `OrderItems`.
  - Successful creation calculates item subtotals and order total amount.
  - Order currency comes from the ordered product currency and defaults to `VND` if blank.
  - Product stock is not reduced during order creation; stock reduction happens during Product Payment Confirmation Placeholder or a future verified payment flow.
  - Real payment gateway, shipping provider, AI chat, subscription quota, and Phase 2 behavior were not added in this slice.
- Product Payment Confirmation Placeholder MVP:
  - Endpoint: `POST /api/orders/{orderId}/confirm-payment`.
  - This endpoint is a mock/payment placeholder for local product order flow testing only.
  - It does not integrate a real payment gateway or verified webhook.
  - Missing or deleted orders return `404 Not Found`.
  - Only orders with `PaymentStatus = Pending` can be confirmed.
  - Already paid/confirmed orders return `409 Conflict`.
  - Before confirming payment, the implementation validates current product stock again for every order item.
  - If current stock is lower than ordered quantity, the endpoint returns `409 Conflict` and does not update order or product stock.
  - Successful confirmation sets `Orders.PaymentStatus = Paid`.
  - Successful confirmation sets `Orders.Status = Processing` so the hospital/platform can move the order into fulfillment/shipping.
  - Successful confirmation reduces each ordered product's `StockQuantity` by the ordered quantity.
  - Order status and product stock updates run in a database transaction.
  - Shipping provider, real payment gateway, AI chat, subscription quota, and Phase 2 behavior were not added in this slice.
- Patient Purchase History MVP:
  - Endpoint: `GET /api/orders/my-orders`.
  - Endpoint: `GET /api/orders/my-orders/{orderId}`.
  - Endpoints are protected with JWT bearer authentication.
  - The current Patient is resolved from the authenticated JWT/current-user claims.
  - The implementation does not trust `patientProfileId` from client input for these "my purchase history" endpoints.
  - Only authenticated `Active` users with the `Patient` role and a non-deleted `PatientProfile` can view purchase history.
  - Unauthenticated requests return `401 Unauthorized`.
  - Authenticated users who are not Active Patients return `403 Forbidden`.
  - Missing orders, deleted orders, or orders belonging to another Patient return `404 Not Found`.
  - My order list returns `orderId`, `orderNumber`, `createdAt`, `totalAmount`, `currency`, `status`, and `paymentStatus`.
  - My order list is sorted by `CreatedAt` descending.
  - My order detail returns `orderId`, `orderNumber`, `createdAt`, `updatedAt`, `shippingAddress`, `totalAmount`, `currency`, `status`, `paymentStatus`, and order items.
  - My order detail items return `orderItemId`, `productId`, `productName`, `quantity`, `unitPrice`, and `subtotal`.
  - Deleted orders and deleted order items are excluded.
  - The response does not expose other users' profile/account data.
  - Swagger/OpenAPI now includes a Bearer JWT security definition for testing protected endpoints.
  - Real payment gateway, shipping provider integration, AI chat, subscription quota, frontend, and Phase 2 behavior were not added in this slice.
- Subscription Purchase Placeholder MVP:
  - Endpoint: `GET /api/subscription-plans`.
  - Endpoint: `GET /api/subscriptions/me`.
  - Endpoint: `POST /api/subscriptions/subscribe`.
  - Endpoint: `POST /api/subscriptions/{subscriptionId}/confirm-payment`.
  - Guests can view active, non-deleted subscription plans.
  - Protected subscription endpoints use JWT bearer authentication and the authenticated current-user claim.
  - Only authenticated `Active` users with the `Patient` role and a non-deleted `PatientProfile` can view/subscribe/confirm their own subscription.
  - Subscribe request body contains `planId`.
  - Subscription plan must exist, be active, and not be deleted.
  - Subscribe creates a `Subscription` owned by the current Patient user.
  - Subscribe stores `Subscriptions.Status = Inactive` internally and returns `status = PendingPayment` in the API response while the linked subscription payment is pending.
  - Subscribe creates a linked `Payments` record with `Purpose = Subscription`, `Status = Pending`, `Currency = VND`, and amount copied from the selected plan.
  - Confirm payment is a local placeholder only; it does not integrate a real payment gateway or verified webhook.
  - Confirm payment only works for the owner Patient and returns `404 Not Found` if the subscription is missing or belongs to another user.
  - Confirm payment rejects already paid/active subscriptions with conflict.
  - Successful confirmation sets the linked subscription payment to `Paid`.
  - Successful confirmation sets `Subscriptions.Status = Active`.
  - Successful confirmation sets `Payments.PaidAt` as the response `startDate`.
  - Successful confirmation sets `Subscriptions.CurrentPeriodEnd` as `startDate + 30 days`.
  - The MVP response uses `VND` and a 30-day duration because the current `SubscriptionPlans` schema does not yet contain explicit `Currency` or `DurationDays` columns.
  - AI chatbot, AI quota enforcement, real payment gateway, frontend, and Phase 2 behavior were not added in this slice.
- Admin Order Management MVP:
  - Endpoint: `GET /api/admin/orders`.
  - Endpoint: `GET /api/admin/orders/{orderId}`.
  - Endpoint: `PUT /api/admin/orders/{orderId}/status`.
  - Admin order list returns non-deleted orders only.
  - Admin order list includes `orderId`, `orderNumber`, `patientProfileId`, `patientName`, `patientEmail`, `status`, `paymentStatus`, `totalAmount`, `currency`, `shippingAddress`, `createdAt`, and `updatedAt`.
  - Optional admin list filters are supported for `status`, `paymentStatus`, `fromDate`, and `toDate`.
  - Admin order detail returns order fields plus order items with `productId`, `productName`, `quantity`, `unitPrice`, and `subtotal`.
  - Admin status update validates the requested `OrderStatus` value.
  - Admin status update request body documents allowed values in Swagger: `Paid`, `Processing`, `Shipped`, `Completed`, and `Cancelled`.
  - The current backend/database enum uses `Completed` for the final delivered/completed state; `Delivered` is not a valid order status in the current schema.
  - `PendingPayment` orders cannot be manually changed to `Completed` before payment is paid.
  - Paid/processing order states can move to fulfillment/completion states according to the current MVP transition rules.
  - Backward transitions are rejected with `Invalid order status transition.`
  - Final statuses such as `Completed` and `Cancelled` cannot be changed.
  - Invalid status values return `Order status is invalid. Allowed values are: Paid, Processing, Shipped, Completed, Cancelled.`
  - Admin cancellation/status update does not restore product stock and does not process refunds in this slice.
  - Real payment gateway, shipping provider integration, AI chat, subscription quota, and Phase 2 behavior were not added in this slice.
- Revenue Report MVP:
  - Endpoint: `GET /api/admin/reports/revenue`.
  - Query parameters: `fromDate`, `toDate`.
  - `fromDate` is required.
  - `toDate` is required.
  - `fromDate` must be before or equal to `toDate`.
  - Response includes `fromDate`, `toDate`, `productRevenue`, `appointmentRevenue`, `totalRevenue`, `paidOrderCount`, `confirmedAppointmentCount`, and `currency`.
  - Product revenue counts only non-deleted product orders with `PaymentStatus = Paid` and non-pending/non-cancelled/non-refunded order status.
  - Appointment revenue counts non-deleted appointments with `Status = Confirmed` or `Status = Completed`.
  - Appointment revenue uses the current linked `MedicalServices.Price` because appointment price snapshot/payment records are not implemented yet.
  - The current MVP date filter uses record `CreatedAt` for both orders and appointments.
  - Default report currency is `VND`.
  - Charts, PDF/Excel export, real payment gateway, AI chat, subscription quota, and Phase 2 behavior were not added in this slice.
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
  - Endpoint: `POST /api/appointments/{appointmentId}/confirm-payment`.
  - Endpoint: `POST /api/appointments/{appointmentId}/cancel`.
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
- Payment Confirmation Placeholder MVP:
  - Endpoint: `POST /api/appointments/{appointmentId}/confirm-payment`.
  - This endpoint is a mock/payment placeholder for local web-flow testing only.
  - It does not integrate a real payment gateway.
  - It does not create payment provider sessions, process webhooks, commission, refunds, or payouts.
  - Only appointments with `Status = PendingPayment` can be confirmed through this placeholder.
  - Missing appointments return `404 Not Found`.
  - Appointments that are not `PendingPayment` return `409 Conflict`.
  - Successful confirmation sets `Appointments.Status = Confirmed`.
  - Successful confirmation sets the related `DoctorScheduleSlots.Status = Booked`.
  - Successful confirmation clears `Appointments.SoftReservedUntil` and `DoctorScheduleSlots.ReservedUntil` because the slot is no longer temporarily reserved after it becomes booked.
- Appointment Cancellation MVP:
  - Endpoint: `POST /api/appointments/{appointmentId}/cancel`.
  - Request body includes `cancellationReason`.
  - Missing or deleted appointments return `404 Not Found`.
  - Already cancelled appointments return `409 Conflict`.
  - `PendingPayment` and `Confirmed` appointments can be cancelled.
  - Successful cancellation sets `Appointments.Status = Cancelled`.
  - Successful cancellation stores `Appointments.CancellationReason`.
  - Successful cancellation clears `Appointments.SoftReservedUntil`.
  - If the related Doctor schedule slot is `SoftReserved` or `Booked`, successful cancellation releases it back to `Available`.
  - Successful cancellation clears `DoctorScheduleSlots.ReservedUntil`.
  - Refund logic, real payment gateway behavior, AI chat, subscription quota, and Phase 2 behavior were not added in this slice.

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
- Implemented Payment Confirmation Placeholder MVP:
  - `POST /api/appointments/{appointmentId}/confirm-payment` confirms a mock payment for an appointment
  - only `PendingPayment` appointments can be confirmed
  - missing appointments return `404 Not Found`
  - non-`PendingPayment` appointments return `409 Conflict`
  - successful confirmation moves the appointment to `Confirmed`
  - successful confirmation moves the related Doctor schedule slot to `Booked`
  - successful confirmation clears `SoftReservedUntil` and slot `ReservedUntil`
  - no real payment gateway, webhook processing, commission, payout, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Payment Confirmation Placeholder:
  - valid `PendingPayment` appointment confirms successfully
  - missing appointment returns not found reason
  - already confirmed appointment returns conflict reason
- Implemented Appointment Cancellation MVP:
  - `POST /api/appointments/{appointmentId}/cancel` cancels an appointment with a reason
  - missing or deleted appointments return `404 Not Found`
  - already cancelled appointments return `409 Conflict`
  - `PendingPayment` appointments can be cancelled
  - `Confirmed` appointments can be cancelled
  - successful cancellation sets appointment status to `Cancelled`
  - successful cancellation saves the cancellation reason
  - successful cancellation clears `SoftReservedUntil`
  - successful cancellation releases `SoftReserved` or `Booked` Doctor schedule slots back to `Available`
  - successful cancellation clears the slot `ReservedUntil`
  - no refund logic, real payment gateway behavior, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Appointment Cancellation:
  - cancel `PendingPayment` appointment succeeds
  - cancel `Confirmed` appointment succeeds
  - cancel non-existing appointment returns not found reason
  - cancel already cancelled appointment returns conflict reason
- Implemented Patient Profile Management MVP:
  - `GET /api/patients/{patientProfileId}/profile` returns safe Patient profile details
  - `PUT /api/patients/{patientProfileId}/profile` updates `dateOfBirth`, `gender`, and `address`
  - missing or deleted Patient profiles return `404 Not Found`
  - profile responses do not expose password hashes or sensitive auth data
  - fixed the EF Core query path for `GET /api/patients/{patientProfileId}/profile` by loading `PatientProfile` with its linked `User` and mapping to `PatientProfileRecord` in memory instead of projecting directly into the record inside the LINQ query
  - verified `GET` and `PUT` profile endpoints against `RehabAIDb`, including not-found cases
  - no database schema changes or migrations were added
  - no AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Patient Profile Management:
  - existing profile can be read
  - missing profile returns null/not found path
  - profile fields can be updated
  - update for non-existing profile returns not found reason
- Implemented Admin Product Management MVP:
  - `GET /api/admin/products` lists non-deleted products for Admin, including inactive products
  - `GET /api/admin/products/{productId}` returns a non-deleted product by id
  - `POST /api/admin/products` creates a hospital/platform-owned product
  - `PUT /api/admin/products/{productId}` updates product fields and respects the provided `isActive` value
  - `DELETE /api/admin/products/{productId}` soft-deletes the product and marks it inactive
  - product validation requires a name, existing non-deleted category, non-negative price, and non-negative stock quantity
  - omitted product currency defaults to `VND`
  - omitted create `isActive` defaults to `true`
  - duplicate generated product slugs return conflict
  - no cart, order, payment gateway, shipping, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Admin Product Management:
  - create defaults missing currency to `VND`
  - create stores active state
  - invalid price or stock returns validation failure
  - missing category returns category-not-found reason
  - duplicate product slug returns duplicate/conflict reason
  - update missing product returns not found reason
  - soft delete existing product succeeds
  - Admin controller create defaults omitted `isActive` to true
  - Admin controller update respects provided `isActive = false`
- Implemented Public Product Listing MVP:
  - `GET /api/products` lists active, non-deleted, in-stock hospital/platform-owned products
  - `GET /api/products/{productId}` returns public product detail only when the product is public-visible
  - public product responses use `productId` and omit admin-only `isActive`
  - optional `keyword` and `categoryId` filters are supported
  - no cart, order, payment gateway, shipping, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Public Product Listing:
  - public list returns only active, in-stock, non-deleted products
  - public detail returns active in-stock product
  - public detail returns null/not-found path for inactive products
  - public detail returns null/not-found path for deleted products
  - keyword and category filters work together
- Implemented Order Creation MVP:
  - `POST /api/orders` creates product orders for Patient profiles
  - `GET /api/orders/{orderId}` returns order detail
  - `GET /api/patients/{patientProfileId}/orders` returns the Patient profile's orders
  - order creation validates patient profile existence, product availability, positive quantity, and stock limits
  - order creation stores `PendingPayment` order status and `Pending` payment status
  - order creation snapshots current product name, unit price, subtotal, total amount, currency, and shipping address
  - product stock is intentionally not reduced at order creation
  - no real payment gateway, shipping provider, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Order Creation:
  - create order succeeds
  - create order fails when Patient profile is missing
  - create order fails when product is missing
  - create order fails when product is inactive
  - create order fails when product is deleted
  - create order fails when quantity is less than or equal to 0
  - create order fails when quantity exceeds product stock
  - get order by id succeeds
  - get Patient orders succeeds
- Implemented Product Payment Confirmation Placeholder MVP:
  - `POST /api/orders/{orderId}/confirm-payment` confirms a mock product order payment
  - only orders with `PaymentStatus = Pending` can be confirmed
  - successful confirmation sets `PaymentStatus = Paid`
  - successful confirmation sets `OrderStatus = Processing`
  - successful confirmation reduces product stock by ordered quantity
  - stock is revalidated immediately before confirmation
  - insufficient stock returns conflict and does not partially update order or stock
  - no real payment gateway, shipping provider, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Product Payment Confirmation Placeholder:
  - confirm payment succeeds
  - confirm payment fails when order is missing
  - confirm payment fails when order is already paid
  - confirm payment fails when stock is insufficient
  - stock decreases after successful confirmation
- Implemented Admin Order Management MVP:
  - `GET /api/admin/orders` lists non-deleted orders with patient/account summary fields
  - `GET /api/admin/orders/{orderId}` returns order detail with order items
  - `PUT /api/admin/orders/{orderId}/status` updates processing status with MVP transition validation
  - optional admin filters are supported for order status, payment status, and created date range
  - invalid order status values return validation errors
  - invalid transitions return conflict-style errors
  - no product stock restoration, refund handling, real payment gateway, shipping provider, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Admin Order Management:
  - admin list orders succeeds and excludes deleted orders
  - admin get order by id succeeds
  - update status succeeds for a valid `Processing` to `Completed` transition
  - update status fails when order is missing
  - update status fails for invalid `PendingPayment` to `Completed` transition
- Improved Admin Order Status validation and Swagger documentation:
  - added a shared order status catalog for backend validation and Swagger request-body documentation
  - `PUT /api/admin/orders/{orderId}/status` Swagger schema now displays allowed status values
  - invalid status values such as `Confirmed` or `Delivered` return a message with allowed values
  - invalid transitions return `Invalid order status transition.`
  - backward transitions are rejected
  - final statuses such as `Completed` and `Cancelled` cannot be changed
- Added unit tests for Admin Order Status validation:
  - invalid status value returns allowed values
  - backward transition returns invalid transition
  - changing a final order status returns invalid transition
- Implemented Revenue Report MVP:
  - `GET /api/admin/reports/revenue` returns revenue statistics for a required date range
  - product revenue is calculated from paid, non-deleted, non-cancelled product orders
  - appointment revenue is calculated from confirmed/completed, non-deleted appointments using linked Medical Service price
  - total revenue is calculated as product revenue plus appointment revenue
  - default report currency is `VND`
  - no charts, PDF/Excel export, real payment gateway, AI chat, subscription quota, or Phase 2 behavior was added in this slice
- Added unit tests for Revenue Report MVP:
  - revenue report succeeds
  - invalid date range returns service validation failure and controller `400 BadRequest`
  - pending orders are excluded
  - cancelled/deleted records are excluded
  - product revenue is calculated correctly
- Implemented Subscription Purchase Placeholder MVP:
  - `GET /api/subscription-plans` lists active, non-deleted subscription plans for Guest/Patient browsing
  - `GET /api/subscriptions/me` returns the authenticated Active Patient's current subscription or `null`
  - `POST /api/subscriptions/subscribe` creates a pending subscription for the authenticated Active Patient
  - `POST /api/subscriptions/{subscriptionId}/confirm-payment` confirms the subscription payment placeholder for the owner Patient
  - subscription payment status is represented through the linked `Payments` record
  - pending subscriptions use `Subscriptions.Status = Inactive` internally and return `PendingPayment` in API responses while payment is pending
  - successful confirmation sets the linked payment to `Paid`, activates the subscription, and sets the subscription end date to 30 days after confirmation
  - no AI chatbot, AI quota enforcement, real payment gateway, frontend, or Phase 2 behavior was added in this slice
- Added unit tests for Subscription Purchase Placeholder:
  - list subscription plans succeeds
  - active Patient can create a pending subscription
  - subscribe fails for missing, inactive, or deleted plans
  - non-Patient/non-active users are blocked from subscription management
  - Patient can view their current subscription
  - confirm payment succeeds and sets start/end dates
  - confirm payment fails when subscription is missing, belongs to another user, or is already paid
- Verified after implementation:
  - `dotnet build RehabAI.sln` passed
  - `dotnet test RehabAI.sln --no-build` passed
- Audited and hardened authentication/authorization before frontend development:
  - added DB-backed policy authorization for active role checks
  - added `ActivePatient`, `ActiveAdmin`, and `ActiveDoctorStaffOrAdmin` policies
  - protected Admin management/reporting endpoints with `ActiveAdmin`
  - protected Patient profile, appointment, order, purchase history, and subscription endpoints with `ActivePatient`
  - protected Doctor schedule and credential scaffold endpoints with `ActiveDoctorStaffOrAdmin`
  - kept public registration, verification, login, doctor discovery, available slots, product listing, and subscription plan listing endpoints public
  - added owner checks for Patient profile, appointment, order, and Doctor schedule access paths
  - updated Swagger security metadata so JWT requirements appear only on protected operations
- Added authorization unit coverage:
  - active Patient role policy succeeds
  - PendingEmail, PendingPasswordSetup, Locked, Suspended, and Deactivated users fail protected policy checks
  - wrong role fails protected policy checks
  - public/protected controller metadata matches intended access-control policy assignments
- Verified authorization hardening:
  - `dotnet build RehabAI.sln` passed
  - `dotnet test RehabAI.sln --no-build` passed
  - local API smoke test confirmed `GET /api/subscription-plans` returns `200` without token
  - local API smoke test confirmed unauthenticated `GET /api/admin/orders` returns `401`
  - local API smoke test confirmed unauthenticated `GET /api/subscriptions/me` returns `401`
  - local Swagger JSON check confirmed public subscription plan listing has no operation-level JWT requirement, while protected admin order listing does

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
POST /api/appointments/{appointmentId}/confirm-payment
POST /api/appointments/{appointmentId}/cancel
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

Payment confirmation placeholder behavior:

```text
POST /api/appointments/{appointmentId}/confirm-payment
```

This placeholder moves `PendingPayment` appointments to `Confirmed`, changes the related schedule slot to `Booked`, and clears both appointment and slot reservation timestamps. It exists only to complete the current web booking flow until real payment gateway/webhook handling is implemented.

Appointment cancellation request body:

```json
{
  "cancellationReason": "Patient needs to reschedule stroke mobility assessment."
}
```

Appointment cancellation behavior:

```text
POST /api/appointments/{appointmentId}/cancel
```

This endpoint allows `PendingPayment` and `Confirmed` appointments to move to `Cancelled`. It clears appointment and slot reservation timestamps and releases a `SoftReserved` or `Booked` slot back to `Available`. Refund/payment reversal is intentionally not implemented yet.

Patient Profile Management implementation files:

```text
src/RehabAI.Api/Controllers/PatientsController.cs
src/RehabAI.Api/Contracts/Patients/PatientProfileRequests.cs
src/RehabAI.Application/PatientProfiles/PatientProfileContracts.cs
src/RehabAI.Application/PatientProfiles/PatientProfileService.cs
src/RehabAI.Infrastructure/PatientProfiles/EfPatientProfileRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/PatientProfiles/PatientProfileServiceTests.cs
```

Patient Profile Management endpoints:

```text
GET /api/patients/{patientProfileId}/profile
PUT /api/patients/{patientProfileId}/profile
```

Patient profile update request body:

```json
{
  "dateOfBirth": "1990-05-20",
  "gender": "Female",
  "address": "Stroke rehabilitation home address"
}
```

The current Patient profile schema does not include stroke-specific fields such as stroke history, affected side, mobility limitations, caregiver notes, or rehabilitation goals. Those fields should be handled in a future schema/migration slice if the product needs structured rehabilitation intake data.

Admin Product Management implementation files:

```text
src/RehabAI.Api/Controllers/AdminController.cs
src/RehabAI.Api/Contracts/Products/ProductRequests.cs
src/RehabAI.Application/Products/ProductContracts.cs
src/RehabAI.Application/Products/ProductManager.cs
src/RehabAI.Infrastructure/Products/EfProductRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Products/ProductManagerTests.cs
tests/RehabAI.UnitTests/Products/AdminProductControllerTests.cs
```

Admin Product Management endpoints:

```text
GET /api/admin/products
GET /api/admin/products/{productId}
POST /api/admin/products
PUT /api/admin/products/{productId}
DELETE /api/admin/products/{productId}
```

Product create request body:

```json
{
  "name": "Stroke Mobility Aid",
  "description": "Support product for stroke mobility training.",
  "categoryId": "guid",
  "price": 450000,
  "currency": "VND",
  "stockQuantity": 10,
  "imageUrl": null,
  "isActive": true
}
```

For create requests, `currency` defaults to `VND` when omitted and `isActive` defaults to `true` when omitted.

Public Product Listing implementation files:

```text
src/RehabAI.Api/Controllers/CommerceController.cs
src/RehabAI.Application/Products/ProductContracts.cs
src/RehabAI.Application/Products/ProductManager.cs
tests/RehabAI.UnitTests/Products/ProductManagerTests.cs
```

Public Product Listing endpoints:

```text
GET /api/products
GET /api/products/{productId}
```

Supported query filters:

```text
keyword
categoryId
```

Public product responses include only browse-safe fields and intentionally omit admin-only fields such as `isActive`.

Order Creation implementation files:

```text
src/RehabAI.Api/Controllers/CommerceController.cs
src/RehabAI.Api/Program.cs
src/RehabAI.Api/Contracts/Orders/OrderRequests.cs
src/RehabAI.Application/Orders/OrderContracts.cs
src/RehabAI.Application/Orders/OrderService.cs
src/RehabAI.Infrastructure/Orders/EfOrderRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Orders/CustomerOrderHistoryControllerTests.cs
tests/RehabAI.UnitTests/Orders/OrderServiceTests.cs
```

Order Creation endpoints:

```text
POST /api/orders
POST /api/orders/{orderId}/confirm-payment
GET /api/orders/{orderId}
GET /api/patients/{patientProfileId}/orders
GET /api/orders/my-orders
GET /api/orders/my-orders/{orderId}
```

Order create request body:

```json
{
  "patientProfileId": "guid",
  "items": [
    {
      "productId": "guid",
      "quantity": 2
    }
  ],
  "shippingAddress": "Stroke rehabilitation home address"
}
```

Order creation creates a `PendingPayment` order and snapshots product price/name into `OrderItems`. Product stock is not reduced until product payment confirmation/payment finalization.

Product payment confirmation placeholder behavior:

```text
POST /api/orders/{orderId}/confirm-payment
```

This placeholder confirms only `PaymentStatus = Pending` product orders. It moves the order to `PaymentStatus = Paid` and `OrderStatus = Processing`, then reduces product stock in the same transaction. It exists only to complete local web-flow testing until real payment gateway/session/webhook handling is implemented.

Patient Purchase History behavior:

```text
GET /api/orders/my-orders
GET /api/orders/my-orders/{orderId}
```

These endpoints use the authenticated JWT/current-user claim to resolve the current Patient. They do not accept or trust `patientProfileId` from the client. Only Active Patient users can access their own product order list and detail. Another patient's order is returned as `404 Not Found` to avoid exposing cross-user data. Deleted orders and deleted order items are excluded.

Admin Order Management implementation files:

```text
src/RehabAI.Api/Controllers/AdminController.cs
src/RehabAI.Api/Contracts/Orders/OrderRequests.cs
src/RehabAI.Api/Swagger/UpdateOrderStatusRequestSchemaFilter.cs
src/RehabAI.Application/Orders/OrderContracts.cs
src/RehabAI.Application/Orders/OrderService.cs
src/RehabAI.Infrastructure/Orders/EfOrderRepository.cs
tests/RehabAI.UnitTests/Orders/OrderServiceTests.cs
```

Admin Order Management endpoints:

```text
GET /api/admin/orders
GET /api/admin/orders/{orderId}
PUT /api/admin/orders/{orderId}/status
```

Supported admin order list query filters:

```text
status
paymentStatus
fromDate
toDate
```

Admin order status update request body:

```json
{
  "status": "Completed"
}
```

Admin order status update is for MVP order processing state only. It does not perform refund handling, stock restoration, shipping provider calls, or real payment gateway work.

Allowed Admin order status update values:

```text
Paid
Processing
Shipped
Completed
Cancelled
```

`Completed` is the current backend/database status for an order that has reached its final delivered/completed state. `Delivered` and appointment-only statuses such as `Confirmed` are not valid order statuses in this schema.

Revenue Report implementation files:

```text
src/RehabAI.Api/Controllers/AdminController.cs
src/RehabAI.Application/Reports/RevenueReportContracts.cs
src/RehabAI.Application/Reports/RevenueReportService.cs
src/RehabAI.Infrastructure/Reports/EfRevenueReportRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Reports/RevenueReportServiceTests.cs
tests/RehabAI.UnitTests/Reports/AdminRevenueReportControllerTests.cs
```

Revenue Report endpoint:

```text
GET /api/admin/reports/revenue?fromDate=2026-05-01T00:00:00+07:00&toDate=2026-05-31T23:59:59+07:00
```

Revenue Report response fields:

```text
fromDate
toDate
productRevenue
appointmentRevenue
totalRevenue
paidOrderCount
confirmedAppointmentCount
currency
```

Current MVP report rules:

```text
Product revenue: paid, non-deleted product orders that are not pending/cancelled/refunded.
Appointment revenue: confirmed/completed, non-deleted appointments using linked MedicalService.Price.
Date range: CreatedAt for current MVP records.
Currency: VND.
```

Subscription Purchase Placeholder implementation files:

```text
src/RehabAI.Api/Controllers/SubscriptionsController.cs
src/RehabAI.Api/Contracts/Subscriptions/SubscriptionRequests.cs
src/RehabAI.Application/Subscriptions/SubscriptionContracts.cs
src/RehabAI.Application/Subscriptions/SubscriptionService.cs
src/RehabAI.Infrastructure/Subscriptions/EfSubscriptionRepository.cs
src/RehabAI.Infrastructure/DependencyInjection.cs
tests/RehabAI.UnitTests/Subscriptions/SubscriptionServiceTests.cs
tests/RehabAI.UnitTests/Subscriptions/SubscriptionsControllerTests.cs
```

Subscription Purchase Placeholder endpoints:

```text
GET /api/subscription-plans
GET /api/subscriptions/me
POST /api/subscriptions/subscribe
POST /api/subscriptions/{subscriptionId}/confirm-payment
```

Subscription subscribe request body:

```json
{
  "planId": "guid"
}
```

Current MVP subscription rules:

```text
Plan listing is public and returns active, non-deleted plans.
Subscription management endpoints require JWT authentication.
Only Active Patient users can subscribe or view/confirm their own subscription.
Subscription payment is represented by a linked Payments row with Purpose = Subscription.
Pending subscription responses use status = PendingPayment while the stored SubscriptionStatus remains Inactive.
Payment confirmation is a placeholder that sets payment Paid and subscription Active.
Current MVP subscription period is 30 days from placeholder confirmation.
Currency defaults to VND because SubscriptionPlans does not yet store Currency.
AI quota enforcement and AI chatbot behavior are intentionally not implemented in this slice.
```

Current access-control policy rules:

```text
Public endpoints:
- POST /api/Auth/register-patient
- POST /api/Auth/verify-email
- POST /api/Auth/login
- POST /api/Auth/setup-doctor-password
- POST /api/Auth/forgot-password
- POST /api/Auth/reset-password
- GET /api/doctors
- GET /api/doctors/{doctorProfileId}
- GET /api/doctors/{doctorProfileId}/available-slots
- GET /api/products
- GET /api/products/{productId}
- GET /api/subscription-plans
- GET /api/medical-services
- GET /api/medical-services/{id}

ActivePatient policy endpoints:
- Patient profile get/update
- Appointment create/detail/history/payment-placeholder/cancel
- Product order create/detail/history/payment-placeholder
- My order purchase history
- Subscription current/subscribe/payment-placeholder

ActiveDoctorStaffOrAdmin policy endpoints:
- Doctor schedule slot management
- Doctor credential/resend-invitation scaffolds
- UC-14 legacy doctor schedule scaffold write endpoint

ActiveAdmin policy endpoints:
- Admin-created Doctor account endpoint
- Admin Medical Services create/update/delete
- Admin Product Management
- Admin Order Management
- Admin Revenue Report
- Admin subscription/payout scaffolds
```

## 6. Git/Branch State

- Current working branch: `Test`.
- Recent completed features should be committed and pushed to `origin/Test`.
- The working tree currently contains uncommitted feature/documentation changes.
- `main` will not show the latest changes until branch `Test` is merged into `main` through a Pull Request.

## 7. Next Recommended Steps

1. Commit/push any uncommitted changes to `origin/Test`.
2. Run the backend API and frontend Vite dev server together, then test Login/Register/Product/Doctor pages against `https://localhost:7007`.
3. Wire Patient Dashboard to real patient profile, appointment, purchase history, and subscription endpoints.
4. Wire Admin Dashboard to product, order, revenue report, medical service, and doctor account endpoints.
5. Test Admin Product Management in Swagger using an existing `ProductCategories.Id`, or add a separate Product Category management/seed slice if product categories need to be managed from the UI.
6. Test Public Product Listing in Swagger with active/inactive/out-of-stock products.
7. Test Order Creation in Swagger with active/inactive/deleted/out-of-stock products.
8. Test Product Payment Confirmation Placeholder in Swagger with paid, pending, and insufficient-stock orders.
9. Test Patient Purchase History in Swagger using a logged-in Active Patient JWT.
10. Test Admin Order Management in Swagger with allowed status values: `Paid`, `Processing`, `Shipped`, `Completed`, and `Cancelled`.
11. Test Revenue Report MVP in Swagger with paid/pending/cancelled/deleted order and appointment cases.
12. Test Subscription Purchase Placeholder in Swagger with a logged-in Active Patient JWT.
13. Implement shipping/delivery tracking fields or workflow from the v6.11 SRS if assigned.
14. Replace appointment/product/subscription payment confirmation placeholders with real payment initialization and verified payment webhook handling.
15. Add refund/payment reversal handling for paid appointment cancellations and paid product orders.
16. Add structured stroke rehabilitation Patient profile/intake fields in a future migration if assigned.
17. AI chatbot and AI/subscription quota enforcement are handled by another team and should not be implemented unless assigned.

## 8. Known Risks

- `docs/database-design.md` still describes the database as a pre-migration design, while the initial migration has now been created and applied. Future documentation updates should keep design and implementation state clearly separated.
- The current database is an initial development database. Future schema changes should be made through new EF Core migrations, not manual SQL edits.
- Seed data is currently inserted at API startup. The seed logic is idempotent, but production deployment should decide whether startup seeding remains acceptable or moves to an explicit deployment/admin initialization step.
- Payment finalization rules depend on verified webhook handling, which still needs implementation.
- Password reset still needs implementation.
- The verification email currently uses the placeholder email sender and includes a raw token in placeholder email content. A real frontend verification URL and production email provider are still needed.
- Development registration responses intentionally expose the raw verification token for Swagger testing only. Production behavior was checked to avoid exposing token helper fields.
- Development Doctor creation responses intentionally expose the raw invitation token for Swagger testing only. Production behavior must continue to avoid exposing invitation token helper fields.
- Admin Product Management create/update requires an existing `ProductCategories.Id`; Product Category management endpoints and product category seed data are not implemented in this slice.
- Public Product Listing only exposes active in-stock products. Product browse, order creation, mock product payment confirmation, and revenue reporting exist, but cart management, shipping/delivery tracking, and real product payment gateway integration are not implemented yet.
- Order Creation validates current product stock but does not reserve or reduce stock yet. The Product Payment Confirmation Placeholder revalidates and reduces stock transactionally, but real payment finalization still must happen through verified webhook handling in production.
- Product Payment Confirmation Placeholder can move orders from `Pending` payment to `Paid` and reduce stock for local web-flow testing, but production payment finalization still must happen through verified gateway/webhook handling.
- Admin Order Management status updates do not restore product stock, process refunds, or call a shipping provider. Those behaviors need explicit future business rules before implementation.
- The current order status model uses `Completed`, not `Delivered`, for the final delivered/completed order state. Adding a separate `Delivered` status would require an explicit future business/schema decision.
- Revenue Report MVP currently filters orders and appointments by `CreatedAt` because real payment `PaidAt`/settlement records are not written by the placeholder payment flows yet.
- Revenue Report appointment revenue uses the current linked `MedicalServices.Price`; appointment/service price snapshots should be added in a future schema slice if historical price accuracy is required.
- Subscription Purchase Placeholder uses the current schema without a new migration. `SubscriptionPlans` does not yet have explicit `Description`, `Currency`, or `DurationDays` columns, so API responses use generated descriptions, `VND`, and a 30-day MVP duration.
- Subscription Purchase Placeholder stores pending subscriptions as `Subscriptions.Status = Inactive` and exposes `PendingPayment` in API responses based on the linked pending `Payments` record. A future schema decision can add a dedicated pending subscription status if needed.
- Subscription payment confirmation is a local placeholder. Production subscription activation should move to real payment initialization and verified payment webhook processing.
- Subscription Purchase Placeholder does not implement AI quota enforcement, AI feature gating, or AI chatbot behavior; those remain assigned to the separate AI/subscription quota workstream.
- Doctor Schedule Slots currently guard against active appointments during update/disable, but appointment booking/rescheduling workflows are not implemented yet.
- Public Doctor listing is intentionally unauthenticated for Guests and Patients, but Admin-only profile approval management is not implemented yet.
- Appointment Booking now moves slots to `SoftReserved`; payment webhook handling still needs to move successful appointments to `Pending` or `Confirmed` and slots to `Booked`.
- Payment Confirmation Placeholder can move appointments from `PendingPayment` to `Confirmed` for local web-flow testing, but production payment finalization still must happen through verified webhook handling.
- Appointment Cancellation can cancel `Confirmed` appointments and release the slot, but refund/payment reversal logic is not implemented yet.
- Patient Profile Management currently supports only existing schema fields (`dateOfBirth`, `gender`, `address`). Stroke-specific rehabilitation notes/intake fields require a future schema decision and migration.
- Pending payment expiration is not implemented yet; expired appointments still need a background job or command to return slots to `Available` and clear `ReservedUntil`.
- Appointment Booking request IDs must be valid GUID strings. Invalid GUID input, such as a copied `scheduleSlotId` with a missing character, is rejected by ASP.NET Core model binding before appointment business rules run.
- `CreateDoctorRequest.YearsOfExperience` is accepted for API compatibility with the current request shape, but it is not persisted because the current `DoctorProfile` schema does not include a `YearsOfExperience` column and this task did not change schema or create a migration.
- Doctor invitation password setup is implemented, but the real frontend setup page and production email URL are still needed.
- Email verification now has unit coverage for valid, invalid, expired, and reused token paths. Broader integration tests against EF Core should still be added before production hardening.
- JWT bearer validation and DB-backed active role policies are now wired for Patient, Admin, and Doctor/Staff/Admin protected endpoint groups. Production deployments must override the development signing key.
- Payment webhook endpoints remain placeholder/public and still need real provider signature verification, idempotency handling, and secret management before production.
- Some UC scaffold endpoints remain non-final placeholders. They are either public browse-only scaffolds or conservatively protected by Patient/Admin/DoctorStaff policies, but their final business logic still needs implementation before production use.
- Fine-grained delegated staff permissions are currently grouped under the seeded internal staff roles used by `ActiveDoctorStaffOrAdmin`; a future permission model can split schedule, credential, support, and finance capabilities more narrowly.
- AI chat quota and guest-to-patient session linking are represented in the schema but still need application-layer enforcement.

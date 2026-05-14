# Rehab AI Project Structure

The solution follows a modular monolith structure:

```text
RehabAI/
  src/
    RehabAI.Api/
      Controllers/
      Contracts/
      Program.cs

    RehabAI.Application/
      Auth/
      Users/
      DoctorApplications/
      Doctors/
      Services/
      Appointments/
      Chatbot/
      Products/
      Cart/
      Orders/
      Payments/
      Subscriptions/
      Disputes/
      Reviews/
      Payouts/
      Reports/
      Emails/
      Common/

    RehabAI.Domain/
      Entities/
      Enums/
      ValueObjects/
      Events/

    RehabAI.Infrastructure/
      Database/
      Identity/
      Email/
      Payment/
      Ai/
      Storage/
      BackgroundJobs/
      Audit/

  tests/
    RehabAI.UnitTests/
    RehabAI.IntegrationTests/

  docs/
```

## Team Split

- Backend core: Auth, Doctor Applications, Doctor search/schedule, Appointments, Orders, Payments, Admin.
- AI teammate: Chatbot, AI client, prompt/context builder, subscription/quota checks, safe AI data access.

## Important Boundary

The chatbot module should not write directly to appointment/payment/order tables. It should read approved public context and redirect/pre-fill flows that use standard backend APIs.

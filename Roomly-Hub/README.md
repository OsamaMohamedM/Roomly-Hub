# Roomly-Hub

> **Roomly-Hub** is a booking platform for guest and host workflows, centered on room listings, booking requests, host approval, secure payments, KYC review, and authenticated user operations.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Npgsql-336791?logo=postgresql)
![Entity%20Framework%20Core](https://img.shields.io/badge/Entity%20Framework%20Core-10.0-512BD4)
![Serilog](https://img.shields.io/badge/Logging-Serilog-1E1E1E)
![Swagger](https://img.shields.io/badge/OpenAPI-Swagger-85EA2D)

---

## 1. Executive Summary

Roomly-Hub is an API-driven booking system designed  marketplace where guests search rooms, create bookings, and complete payments while hosts manage listings, approve or reject requests, and control room availability.

The platform focuses on the core operational needs of a modern booking domain:

- guest booking lifecycle management,
- host-controlled request approval flows,
- room publishing, moderation, activation, and photo management,
- secure authentication and account recovery,
- payment initiation, refund handling, and webhook reconciliation,
- and strong auditability across all transactional workflows.

Roomly-Hub is built to keep booking, payment, and state-transition logic explicit, testable, and safe under concurrent usage.

---

## 2. Core Functional Features

### Authentication and Account Management

Roomly-Hub includes a full authentication and identity workflow:

- user registration,
- email verification,
- login and JWT token issuance,
- refresh token rotation,
- Google login,
- forgot password and reset password flows,
- account unlock requests,
- logout,
- and user profile operations.

### Room Management

Hosts can manage rooms through a complete lifecycle:

- create room listings,
- update room details,
- upload and remove room photos,
- submit a room for review,
- activate or deactivate a room,
- delete a room,
- search rooms using filters,
- and inspect room availability.

### Booking Lifecycle

The booking module supports both direct booking and host-approval workflows.

Key operations include:

- create booking,
- update booking dates,
- cancel booking,
- view guest bookings,
- list host booking requests,
- load a host room calendar,
- approve booking requests,
- and reject booking requests.

The codebase supports host-approval rooms where multiple requests can exist for the same date range and are handled through booking state and payment flow rather than being blocked too early.

### Payment Processing

Roomly-Hub integrates with the Fawaterk payment gateway for invoice creation and payment tracking.

Supported payment operations include:

- retrieving available payment methods,
- creating payment invoices for bookings,
- tracking booking payment status,
- refunding paid bookings,
- and processing gateway webhooks for payment success, failure, and cancellation.

### KYC and Moderation

The application includes supporting workflows for trust and review:

- KYC submission,
- KYC review and moderation,
- and room moderation flows.

### Audit and Reliability

The system persists webhook events and uses consistent error handling so payment and booking operations remain traceable, idempotent, and concurrency-safe.

---

## 3. Technical Architecture

Roomly-Hub follows **Clean Architecture** with clear separation between domain rules, application workflows, infrastructure concerns, and HTTP delivery.

### Domain Layer

The Domain layer holds the business model:

- booking entities,
- room entities,
- authentication entities,
- payment-related entities,
- value-like models and enums,
- and invariant-bearing methods such as booking and payment state transitions.

This layer is kept free from framework-specific dependencies wherever possible.

### Application Layer

The Application layer coordinates use cases and business orchestration.

It contains:

- service interfaces,
- command and query services,
- DTOs,
- validators,
- mappers,
- result handling,
- and shared application constants and helpers.

The application layer is where booking rules, validation, authorization checks, and payment workflow orchestration are implemented.

### Infrastructure Layer

The Infrastructure layer provides technical implementations:

- Entity Framework Core persistence,
- PostgreSQL integration through Npgsql,
- repository implementations,
- unit of work implementation,
- Fawaterk gateway integration,
- Google authentication support,
- email delivery support,
- logging integration,
- and supporting services such as token generation and hashing.

### API Layer

The API layer exposes HTTP endpoints through ASP.NET Core controllers:

- authentication endpoints,
- room endpoints,
- booking endpoints,
- payment endpoints,
- KYC endpoints,
- user endpoints,
- and webhook endpoints.

Controllers remain thin and primarily translate HTTP requests into application-service calls and structured HTTP responses.

### CQRS-Style Separation

Roomly-Hub uses a CQRS-style structure, but **MediatR is not used**.

Instead, the codebase separates responsibilities through dedicated services such as:

- `IBookingCommandService`,
- `IBookingQueryService`,
- `IBookingPaymentFlowService`,
- `IRoomService`,
- `IAuthService`,
- and `IPaymentWebhookService`.

This gives the same practical separation benefits as CQRS while keeping the pipeline explicit and lightweight.

### Result Pattern for Error Handling

The application layer uses a `Result` pattern to represent expected business outcomes without relying on exceptions for normal control flow.

Typical outcomes include:

- success,
- validation failure,
- unauthorized action,
- not found,
- invalid state,
- and concurrency conflict.

This makes controller-to-service interactions predictable and keeps HTTP mapping straightforward.

### Validation with FluentValidation

Incoming DTOs and command models are validated with FluentValidation.

Validation is used for:

- required inputs,
- format checks,
- date and state constraints,
- and request consistency before any transactional work begins.

### Concurrency Handling

Concurrency is handled through transactional execution and explicit conflict handling.

The codebase uses:

- optimistic concurrency checks,
- `xmin`-based concurrency support in PostgreSQL migrations,
- transaction scopes through `IUnitOfWork.ExecuteInTransactionAsync`,
- and `ConcurrencyException` mapping to domain-level failures.

This is critical for bookings, approvals, payments, and refund flows where simultaneous requests can occur.

### Repository and Unit of Work Patterns

Roomly-Hub uses repositories for data access and a unit of work for transactional boundaries.

This provides:

- centralized persistence logic,
- reusable queries,
- atomic updates across aggregates,
- and easier testability.

### Webhook Logging and Idempotency

Payment webhooks are persisted through a dedicated `PaymentWebhookLog` model and repository.

This supports:

- auditability,
- duplicate detection,
- replay analysis,
- and safe retry handling for gateway callbacks.

---

## 4. Technology Stack

### Backend

- **.NET 10**
- **ASP.NET Core Web API**
- **Entity Framework Core 10**
- **FluentValidation**
- **Newtonsoft.Json**
- **Serilog**
- **Swashbuckle / OpenAPI**

### Database

- **PostgreSQL**
- **Npgsql.EntityFrameworkCore.PostgreSQL**

### Authentication and Security

- **JWT Bearer authentication**
- **Google authentication**
- **OTP-based email verification**
- **refresh tokens**
- **BCrypt password hashing**
- **role and permission checks**

### Payment and External Services

- **Fawaterk Payment Gateway**
- **email delivery via MailKit**
- **local webhook testing via ngrok**

### Supporting Infrastructure Packages

- **Hangfire.PostgreSql**
- **StackExchange.Redis**
- **Microsoft.EntityFrameworkCore.Design / Tools**

---

## 5. Webhook & Payment Lifecycle

Roomly-Hub uses a secure payment flow centered on invoice generation and webhook reconciliation.

### 1. Invoice Creation

When a guest initiates payment for a booking, the system generates a payment invoice through Fawaterk.

The invoice creation flow stores payment references back on the booking so later events can be reconciled.

### 2. Payment Method Retrieval

The API can request available payment methods from the gateway and expose them to the client.

### 3. Booking Payment Status

The platform can return the current booking payment state so the client can reflect whether the booking is still awaiting payment, paid, refunded, or otherwise transitioned.

### 4. Webhook Reception

The webhook controller receives gateway callbacks for:

- successful payment,
- failed payment,
- and cancellation events.

### 5. Signature Verification

Each webhook is verified using gateway-specific hash logic before any state change is allowed.

### 6. Idempotency

Webhook processing is idempotent.

If the booking is already paid or already cancelled, the service returns success without applying the same transition twice.

### 7. Logging and Persistence

Every webhook is written to `PaymentWebhookLogs` with:

- invoice or reference metadata,
- webhook type,
- raw payload,
- booking linkage,
- processing status,
- timestamps,
- and any processing error.

### 8. Booking State Rules

Failed payment webhooks do not mark the booking as failed.

The booking remains payable so the guest can retry payment later.

### 9. Refund Flow

Refunds are handled separately through the payment/booking service layer and are only allowed when the booking is already in a paid state.

---

## 6. Project Standards

Roomly-Hub follows a set of explicit engineering standards that shape how code is written, organized, and evolved.

### Clean Architecture

The solution is split into `Domain`, `Application`, `Infrastructure`, and API projects.

This separation keeps business rules independent from technical details and makes the codebase easier to maintain.

### SOLID Principles

The codebase is structured around the SOLID principles:

- **Single Responsibility**: controllers, services, and repositories each have focused responsibilities,
- **Open/Closed**: behavior is extended through new services and handlers rather than rewriting existing ones,
- **Liskov Substitution**: abstractions are used consistently through interfaces,
- **Interface Segregation**: service interfaces are separated by use case,
- **Dependency Inversion**: higher-level modules depend on abstractions, not concrete infrastructure.

### DRY

Common logic is centralized into reusable services, validators, helpers, and shared error/result types.

This reduces duplication across controllers and application workflows.

### Inversion of Control and Dependency Injection

The application uses built-in ASP.NET Core DI to wire services, repositories, validators, and infrastructure dependencies.

Controllers depend on interfaces rather than concrete classes.

### CQRS-Style Structure

The project uses command/query separation through dedicated services instead of MediatR.

This keeps the workflow explicit and avoids an extra messaging layer.

### Result-Driven Application Flow

Expected business outcomes are represented with `Result` objects rather than exception-driven branching.

This is used consistently across booking, room, auth, and payment workflows.

### Validation-First Design

Requests are validated before state changes occur.

FluentValidation is used throughout the application layer to prevent invalid data from reaching the domain and persistence layers.

### Transactional Consistency

Important operations are wrapped in Unit of Work execution blocks.

This ensures booking updates, payment transitions, and webhook state changes remain atomic.

### Concurrency Safety

The project uses optimistic concurrency and explicit conflict handling.

This is especially important for bookings, host approvals, cancellation, and payment reconciliation.

### Structured Logging

Serilog and `ILogger<T>` are used for operational observability.

Important actions such as authentication, booking changes, payment creation, and webhook processing are logged with context.

### Secure-by-Default Web and Payment Behavior

The codebase includes:

- JWT-based authentication,
- role and ownership checks,
- webhook signature verification,
- and strict state validation before critical actions.

### Booking Domain Rules

The booking domain is implemented with explicit rules for:

- awaiting payment,
- pending host approval,
- confirmed bookings,
- cancellations,
- refunds,
- and room availability checks.

### Payment Retry Policy

Failed payment webhooks are intentionally non-destructive.

The booking stays payable, which aligns with the requirement that guests can retry payment after a failure.

### Framework and Library Choices Used in This Codebase

The current codebase uses the following implementation technologies and libraries:

- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL / Npgsql
- FluentValidation
- Serilog
- Newtonsoft.Json
- Swashbuckle / OpenAPI
- JWT Bearer authentication
- Google authentication
- MailKit
- Hangfire.PostgreSql
- StackExchange.Redis
- BCrypt.Net-Next
- `ProblemDetails`-based API error responses

---

## 7. Getting Started

### Prerequisites

Ensure the following are installed locally:

- .NET 10 SDK
- PostgreSQL
- Visual Studio or Visual Studio Code
- Git
- ngrok for webhook testing

### Setup Steps

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd Roomly-Hub
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Configure application settings**

   Update `appsettings.json` and environment-specific configuration with the following sections:

   - `ConnectionStrings:DefaultConnection`
   - `JwtSettings`
   - `EmailSettings`
   - `GoogleSettings`
   - `Fawaterak`

4. **Apply database migrations**

   ```bash
   dotnet ef database update -p Infrastructure -s Roomly-Hub
   ```

5. **Run the API**

   ```bash
   dotnet run --project Roomly-Hub
   ```

6. **Expose the webhook endpoint locally**

   Use `ngrok` to expose the local API to the payment gateway so webhooks can reach the webhook controller.

### Recommended Local Validation Flow

- register and verify a user,
- create a room,
- create a booking,
- initiate payment,
- confirm the invoice is stored on the booking,
- send webhook callbacks through ngrok,
- and verify the booking state and webhook logs.

---

## Repository Structure

The solution is organized into the following projects:

- `Domain`
- `Application`
- `Infrastructure`
- `Roomly-Hub`

---

## Closing Note

Roomly-Hub is designed as a practical, production-oriented booking platform with explicit booking state management, secure payments, and clear architectural boundaries.

Its codebase emphasizes reliability, maintainability, and predictable behavior across booking, payment, authentication, and moderation workflows.
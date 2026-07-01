# ⚡ BillingFlow - Enterprise CRM & Billing Platform

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C# 12](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC292B?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7.4-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![NGINX](https://img.shields.io/badge/NGINX-Reverse_Proxy-009639?style=for-the-badge&logo=nginx&logoColor=white)
![Stripe](https://img.shields.io/badge/Stripe-Integration-008CDD?style=for-the-badge&logo=stripe&logoColor=white)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Integration-F35426?style=for-the-badge&logo=opentelemetry&logoColor=white)
![Grafana](https://img.shields.io/badge/Grafana-Observability-F46800?style=for-the-badge&logo=grafana&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![CI/CD](https://img.shields.io/badge/GitHub-Actions-2088FF?style=for-the-badge&logo=github-actions&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

> **BillingFlow** is an enterprise-oriented CRM and billing platform built on a modern **.NET 8** stack.  
> It combines **Domain-Driven Design (DDD)**, **CQRS**, **Event-Driven Architecture (EDA)**, **Transactional Outbox**, **Hangfire**, **SignalR**, **NGINX**, and **Stripe** integration within a clean architecture, supported by a reliable, idempotent event dispatch flow and a centralized observability stack based on OpenTelemetry and the Grafana ecosystem.

---

## 🏗️ Architecture & Core Principles

BillingFlow follows **Clean Architecture** with **Vertical Slice** feature organization. Business rules live in the domain and application layers, while infrastructure concerns stay isolated.

- **CQRS** - write operations mutate rich domain aggregates through Entity Framework Core; read operations use Dapper or raw SQL for fast, purpose-built queries.
- **DDD** - aggregates such as `Invoice`, `Payment`, `Client`, `ProvidedService`, and `AppUser` protect invariants and emit domain events.
- **Transactional Outbox & Idempotency** - domain changes and integration events are persisted within the same database transaction, reducing the risk of lost side effects. A concurrent-safe background relay worker processes pending messages asynchronously, routing them to an idempotent consumer flow that uses lease-based tokens to coordinate dispatch ownership while supporting event fan-out.
- **Post-Save Notification Queue** - decouples non-critical external I/O from the database transaction boundary. Best-effort real-time notifications (SignalR) are buffered in memory and processed after `SaveChangesAsync` succeeds, helping prevent slow network connections or Redis operations from blocking primary database connections.
- **Edge Gateway & Security** - NGINX acts as a reverse proxy for API and SignalR traffic, with forwarded headers restricted to trusted proxy networks.
- **Rate Limiting** - layered rate limiting combines a global API limiter with endpoint-specific policies for sensitive workflows.
- **Idempotency & Concurrency** - Stripe webhook delivery, payment attempts, and token handling are protected with unique constraints, row versions, and explicit state transitions.
- **Authorization** - JWT-based authentication is combined with permissions and policy-based checks. Customer-facing reads use row-level access rules inside handlers.
- **Auditing** - EF Core interceptors capture JSON deltas for entity changes and correlate them with distributed trace identifiers.
- **Observability & Telemetry** - configured with OpenTelemetry and Serilog, with traces, metrics, and structured logs routed through Grafana Alloy to Prometheus, Loki, and Tempo.
- **Real-time** - SignalR pushes payment updates to the frontend, backed by Redis for horizontal scaling.
- **Background Processing** - Hangfire handles recurring compliance and maintenance jobs without blocking request flow.

---

## 🗄️ Data Model & ERD

The system is centered around SQL Server tables mapped with EF Core and managed through FluentMigrator.

![BillingFlow Entity Relationship Diagram](./docs/ERD.png)

### Identity & Security
* **Users** - system identities, roles, password hashes, and lifecycle state.
* **UserTokens** - refresh tokens, password reset tokens, replay-detection support, and session tracking.

### CRM & Billing
* **Clients** - billing profiles, soft-delete lifecycle, and optional 1:1 linkage to `Users`.
* **ProvidedServices** - billable work recorded before invoicing.
* **Invoices** and **InvoiceItems** - the core financial aggregates that control billing state.

### Payments
* **PaymentAttempts** - reservation records for online payment sessions.
* **Payments** - immutable ledger entries for manual and online transactions.
* **StripeEventLogs** - webhook idempotency and deduplication records.

### Read Models & Infrastructure
* **ClientBalances** - materialized debt projection updated from payment events.
* **AuditLogs** - infrastructure-level audit trail with old/new values and request metadata.
* **OutboxMessages** - persistent integration log for background event dispatching.
* **IntegrationDispatchLogs** - lease-based state tracking log to prevent duplicate downstream side-effect execution.

---

## 🚀 Domain Modules

### 🛡️ Identity & Security
* Secure JWT issuance with refresh-token rotation.
* Replay-attack detection for reused refresh tokens.
* Hierarchical role management for admin and back-office users.
* Global and endpoint-specific rate limiting policies protect API resources and sensitive authentication flows.
* Account recovery and verification emails are dispatched via the Outbox pattern.

### 🏢 Client Management
* Clients move through an explicit lifecycle: `Active`, `Suspended`, and `Archived`.
* Archiving safely unlinks the underlying user account to avoid future uniqueness conflicts.
* Query filters keep archived clients out of normal reads.

### 📄 Billing & Invoicing
* Invoices begin as `Draft` and transition to `Unpaid` on issuance.
* Payments can move an invoice to `PartiallyPaid`, `Paid`, or `Overdue`.
* Invoice generation pulls unbilled services into an invoice and marks them billed.
* PDF generation is handled asynchronously through QuestPDF.

### 💳 Payment Processing
* Online payments use a reservation-based workflow through `PaymentAttempt`.
* Stripe webhook processing is protected by cryptographic signature validation.
* Successful payments emit a `PaymentRecordedEvent`, which updates the `ClientBalances` read model using raw SQL UPSERT logic.

### ⚙️ Background Jobs & Real-Time
* `ProcessOutboxMessagesJob` relays pending integration events to external providers.
* `CleanupExpiredTokensJob` removes expired tokens.
* `CheckOverdueInvoicesJob` marks overdue invoices.
* `SuspendOverdueClientsJob` enforces compliance rules.
* SignalR publishes payment updates to connected users in real time.

---

## 🛠️ Technology Stack

| Category | Technology |
| --- | --- |
| Platform | .NET 8, C# 12, ASP.NET Core Web API |
| Reverse Proxy | NGINX |
| Write Model | Entity Framework Core 8 |
| Read Model | Dapper, raw SQL, stored procedures |
| Database | Microsoft SQL Server 2022 |
| Migrations | FluentMigrator |
| Messaging | MediatR |
| Background Jobs | Hangfire |
| Real-Time | SignalR + StackExchange.Redis |
| Observability | OpenTelemetry, Serilog, Grafana Alloy, Prometheus, Loki, Tempo |
| External APIs | Stripe.net |
| PDF Generation | QuestPDF |
| Validation | FluentValidation |
| Security | JWT Bearer, BCrypt.Net |
| Testing | xUnit, Moq, FluentAssertions, MockQueryable |
| Integration Testing | Testcontainers, Respawn |

---

## 🚦 Getting Started

The environment is containerized. A single command provisions NGINX, SQL Server, Redis, applies migrations, and starts the API with the full observability stack.

### 1. Environment Setup
Copy the template configuration file and fill in the required secrets.

```bash
cp .env.example .env
```

Make sure `STRIPE_SECRET_KEY`, `GRAFANA_ADMIN_PASSWORD` and the other required variables are set correctly.

### 2. Start the Stack

```bash
docker compose up --build
```

The stack starts the edge gateway (NGINX), database, Redis, telemetry stack, migrator, API, and Stripe CLI tunnel. Wait for the migrator to finish before running flows that depend on the schema.

### 3. Stripe Webhook Configuration
The local Stripe CLI container prints a webhook signing secret (`whsec_...`).

Add that value to `.env` under `STRIPE_WEBHOOK_SECRET`, then restart the API container so the new secret is loaded.

### 4. Swagger / API Docs
When the API is running, open the interactive OpenAPI UI through the NGINX gateway:

```text
http://localhost/swagger/index.html
```

Paste a raw JWT into Swagger's **Authorize** dialog. The UI injects the `Bearer ` prefix automatically.

### 5. Observability Dashboards (Grafana)
Navigate to the local Grafana instance to monitor Logs, Metrics, and Traces:

```text
http://localhost:3000
```

* Username: admin
* Password: Value of `GRAFANA_ADMIN_PASSWORD` from your `.env` file.

Use the Explore tab to:
* query structured logs in Loki `({service_name="BillingFlow.Api"} | json)`,
* visualize distributed traces in Tempo,
* correlate traces and logs using `TraceId`, 
* monitor performance metrics in Prometheus.

### 6. Postman
Import the provided [Postman collection](./docs/BillingFlow.postman_collection.json) to exercise end-to-end flows.

The collection is prepared for:
* login and token capture,
* refresh-token rotation,
* authorized API requests,
* invoice/payment workflows.

---

## 🧪 Testing Strategy

* **Domain tests** validate aggregate invariants in isolation.
* **Application tests** verify handlers, policies, and command/query behavior with mocks.
* **Integration tests** use Testcontainers to run against a real SQL Server instance.
* **Respawn** resets database state between tests for deterministic execution.
* **Concurrency tests** exercise raw SQL UPSERT logic and webhook idempotency under parallel execution.

---

## 🔮 Future Roadmap

* Localization and currency formatting for PDFs and UI.
* Back-office SPA and customer portal.
* Production email provider integration.
* Additional payment providers alongside Stripe.

---

## 📜 License

This project is licensed under the MIT License. See the `LICENSE` file for details.

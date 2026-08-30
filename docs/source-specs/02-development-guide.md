# EBOSP — Comprehensive Development Module & End-to-End Implementation Guide

Enterprise Business Operations & Security Platform | Development Standard, Workflow, Quality Gates and Production Handover

Technology baseline: C# / ASP.NET Core + React / TypeScript + PostgreSQL or SQL Server + Azure + Docker + CI/CD

Version: 1.0 | Development lifecycle: requirements → design → implementation → integration → testing → security → deployment → operations → handover

> Transcribed verbatim (text content) from `EBOSP_Comprehensive_Development_Module_and_End_to_End_Development_Guide.pdf`. This is the master engineering playbook — follow it phase by phase. Companion to `01-system-design-specification.md`.

---

## 1. Purpose of This Development Manual

This document converts the EBOSP system specification into an executable development programme. It is intended to be used as the master engineering playbook from the first repository commit through production release and handover. Every module must follow the same discipline: understand the requirement, model the domain, define the contract, implement backend rules, implement frontend behavior, add events/audit/security controls, test the behavior, document it, deploy it, observe it and obtain a release gate.

- Do not begin by building screens. Begin with business rules, entities, workflows, API contracts and acceptance criteria.
- Do not trust the frontend for security. Every authorization and validation rule must be enforced on the server.
- Do not modify stock, payments or other high-value state through ad-hoc updates. Use explicit business commands and audited transactions.
- Do not publish an event before the corresponding database transaction is safely committed. Use the Outbox pattern.
- Do not mark a feature complete until tests, audit behavior, authorization, error handling, documentation and deployment impact are covered.
- Prefer a modular monolith for the first production release. Extract services only when a concrete scalability, isolation or organizational requirement exists.

## 2. Definition of Done — Mandatory for Every Feature

| Area | A feature is not Done until... |
|---|---|
| Requirement | User story, business rules, edge cases and acceptance criteria are documented and approved. |
| Design | Domain impact, database changes, API contract, UI behavior, events and security impact are documented. |
| Backend | Use case, validation, authorization, transaction boundary and error handling are implemented. |
| Database | Migration, indexes, constraints and rollback/recovery considerations are reviewed. |
| Frontend | Loading, success, empty, validation, authorization, error and responsive states are implemented. |
| Events | Required domain/integration events, outbox records and idempotent consumers are implemented. |
| Audit | High-value actions have actor, timestamp, tenant, resource, outcome and correlation information. |
| Security | Threat/abuse cases and authorization tests exist; secrets are not committed. |
| Tests | Unit + integration/API tests cover normal and negative paths; E2E covers critical journeys. |
| Observability | Logs, metrics, health behavior and correlation IDs exist where appropriate. |
| Documentation | README/API/module documentation and configuration changes are updated. |
| Review | Code review completed; CI is green; no critical/high unresolved security defects. |
| Deployment | Feature is deployed to staging and smoke-tested before production. |
| Acceptance | Product/business owner validates acceptance criteria. |

## 3. Master Development Lifecycle

1. Product discovery and requirements clarification
2. Business process modelling and event identification
3. Domain and data modelling
4. Architecture and technical design
5. Repository and developer environment setup
6. Backend foundation
7. Frontend foundation
8. Identity, tenant isolation and authorization
9. Core modules in dependency order
10. Eventing, Outbox and background processing
11. Security hardening
12. Testing and quality automation
13. Observability and operational readiness
14. Containerization and CI/CD
15. Staging deployment and acceptance
16. Production deployment
17. Post-release verification
18. Documentation and handover
19. Maintenance, monitoring and controlled future enhancements

## 4. Recommended Build Order

| Phase | Build | Reason |
|---|---|---|
| 0 | Requirements, architecture, coding standards | Prevents rework |
| 1 | Repository, CI, configuration, database foundation | Creates a safe engineering base |
| 2 | Identity, tenant, users, roles, permissions | All later modules depend on authorization |
| 3 | Branches, warehouses, products, master data | Required by inventory and transactions |
| 4 | Inventory ledger and balances | Foundation for procurement and sales |
| 5 | Procurement + approval workflow | Feeds inventory |
| 6 | Sales + fulfillment | Consumes inventory |
| 7 | Billing + payments | Completes financial flow |
| 8 | Audit + security analytics | Cross-cutting protection |
| 9 | Notifications + SignalR + documents | Improves operations |
| 10 | Reports/read models | Management visibility |
| 11 | AI assistant (read-only) | Portfolio differentiator after reliable data foundation |
| 12 | Performance, resilience, security and E2E hardening | Production readiness |
| 13 | Azure deployment + monitoring + DR verification | Release |
| 14 | Handover + backlog | Sustainable operation |

## 5. Repository and Solution Structure

Use a single repository initially. Keep backend and frontend independently buildable while sharing API contracts through OpenAPI/generated types where practical.

```
/
  src/
    EBOSP.Api/
    EBOSP.Application/
    EBOSP.Domain/
    EBOSP.Infrastructure/
    EBOSP.Contracts/
    EBOSP.Worker/
  frontend/
    src/
      app/
      auth/
      components/
      features/
        inventory/
        procurement/
        sales/
        finance/
        security/
        reports/
        documents/
      services/
      hooks/
      routes/
      types/
  tests/
    EBOSP.UnitTests/
    EBOSP.IntegrationTests/
    EBOSP.ApiTests/
    EBOSP.E2ETests/
  docs/
    architecture/
    api/
    modules/
    runbooks/
    decisions/
  infra/
    docker/
    azure/
  scripts/
  .github/workflows/
```

- **Domain:** entities, value objects, domain events and domain invariants only.
- **Application:** commands, queries, use cases, validators, policies, orchestration and interfaces.
- **Infrastructure:** EF Core, database implementation, messaging, storage, email/SMS adapters and external integrations.
- **API:** HTTP endpoints, authentication plumbing, filters, serialization, OpenAPI and request/response mapping.
- **Worker:** asynchronous event consumers, notification processing, analytics jobs and scheduled tasks.
- **Contracts:** stable DTOs, event contracts and integration schemas; never expose internal entities directly.
- **Frontend:** feature-oriented React components, API clients, route guards, forms and state/query management.

## 6. Branching and Git Workflow

- `main`: production-ready code only.
- `develop` (optional): integration branch if the team needs it; otherwise short-lived branches merge directly into protected `main`.
- `feature/<area>-<short-description>`: feature work.
- `fix/<area>-<short-description>`: defect fixes.
- `hotfix/<incident>`: urgent production fixes.
- Every pull request must describe purpose, business behavior, database changes, API changes, security impact and testing performed.
- Never commit passwords, API keys, tokens, production connection strings, customer data or generated secrets.
- Require CI to pass before merge and protect main with review rules.

## 7. Development Environment Setup

1. Install the required .NET SDK version selected by the project baseline.
2. Install Node.js LTS and the package manager selected for the React project.
3. Install PostgreSQL or SQL Server locally, preferably through Docker for reproducibility.
4. Install Docker Desktop/Engine.
5. Install Git and configure signing/identity according to team policy.
6. Clone the repository and create a local environment file from the documented template.
7. Start infrastructure dependencies using the project compose/development scripts.
8. Apply database migrations.
9. Seed only synthetic development data.
10. Run backend unit/integration tests.
11. Run frontend lint, type checks and tests.
12. Start API and frontend and execute the smoke-test checklist.

## 8. Configuration and Secrets

- Configuration hierarchy: safe defaults → environment-specific configuration → deployment secrets.
- Never store secrets in appsettings committed to Git.
- Use environment variables locally and a managed secret store in Azure production.
- Separate development, test, staging and production configurations.
- Configuration should be strongly typed and validated at application startup.
- Fail fast when mandatory production configuration is absent.
- Do not log connection strings, authorization headers, refresh tokens or credentials.

**Example configuration categories:** Database, Identity, JWT/session, Storage, Messaging, Email/SMS providers, Payment provider, Allowed origins, Rate limits, Feature flags, Telemetry, AI provider.

## 9. Foundation Module

Before business features, implement cross-cutting infrastructure.

- Global exception handling and standardized error responses.
- Correlation/request ID middleware.
- API versioning strategy.
- OpenAPI/Swagger generation.
- Health endpoints.
- Structured logging.
- Validation pipeline.
- Authorization/policy framework.
- Database transaction abstraction.
- Outbox infrastructure.
- Pagination/filter/sort conventions.
- Idempotency support for retry-sensitive commands.
- Clock abstraction so time-dependent business rules are testable.
- Current-user/tenant context abstraction.

## 10. Database Development Workflow

1. Create/modify domain model.
2. Review ownership and tenant scope.
3. Define primary/foreign keys and constraints.
4. Choose appropriate data types; use decimal/numeric for money.
5. Add unique constraints and indexes based on query patterns.
6. Create EF Core migration.
7. Review generated SQL/migration for destructive operations.
8. Apply migration to local development database.
9. Run integration tests.
10. Test migration on a clean database.
11. Test migration against a representative staging backup/snapshot where appropriate.
12. Document any manual data migration.
13. Deploy schema changes before application changes when backward compatibility requires it.

- Persist timestamps in UTC.
- Do not allow ordinary business code to update immutable ledger/audit history.
- Use optimistic concurrency on highly contended records.
- Use transactions for multi-table business operations.
- Keep reporting queries from damaging transactional performance.

## 11. Identity & Access Module

This module is built first because every subsequent feature depends on identity and authorization.

- Implement user lifecycle: create, activate, suspend, disable and recover.
- Implement login/logout/session/token lifecycle.
- Implement role and permission management.
- Implement tenant and branch scope.
- Add MFA-ready architecture; production MFA should be enabled according to risk and organizational policy.
- Implement failed-login rate limiting and audit events.
- Implement password reset/recovery without revealing whether an account exists.
- Implement session/token revocation for suspicious or administrative actions.
- Create policy handlers for sensitive operations.

### 11.1 Authentication Flow

1. User submits credentials.
2. Identity service validates credentials.
3. Risk controls evaluate the request.
4. MFA challenge is performed when required.
5. Identity service issues the approved session/access representation.
6. Frontend stores only what the security model permits; avoid unsafe token storage patterns.
7. API validates identity on every protected request.
8. Authorization policy evaluates role, permission and tenant/branch scope.
9. Business service executes only after authorization succeeds.
10. Security/audit event is recorded.

### 11.2 Authorization Test Matrix

| Scenario | Expected result |
|---|---|
| Unauthenticated request to protected endpoint | 401 |
| Authenticated user without permission | 403 |
| User accesses another tenant's resource | Denied |
| User accesses unauthorized branch | Denied |
| User attempts self-approval where prohibited | Denied |
| Admin performs high-risk action | Allowed + audit |
| Suspended user attempts login | Denied + audit |
| Expired/revoked session | Denied |
| Tampered client-side role | Server ignores client claim changes not backed by trusted identity |

## 12. Tenant, Branch and Master Data Module

- Create tenant/company.
- Create branches and warehouses.
- Define users' tenant and scope membership.
- Create product categories and products.
- Define SKU uniqueness within the required scope.
- Configure tax, pricing and reorder rules.
- Seed reference data through controlled migrations or seed scripts.
- Ensure all queries automatically apply tenant scope.

**Critical rule:** tenant isolation is a security boundary, not merely a UI filter.

## 13. Inventory Module — Detailed Development

Inventory is the most important operational foundation because procurement and sales both change stock.

### 13.1 Core entities

Product, ProductCategory, Warehouse, StockBalance, StockLedgerEntry, StockReservation, StockTransfer, GoodsReceipt, StockAdjustment, ReorderRule.

### 13.2 Stock transaction rules

- Every stock movement has a source, actor, timestamp, tenant, warehouse, product, quantity and reason/reference.
- Never overwrite historical stock movements.
- Current balance may be stored as a projection for performance, but the ledger remains the audit history.
- Prevent duplicate receipt/issue processing with idempotency/reference constraints.
- Negative stock must be governed by an explicit business policy.
- Large adjustments require additional authorization.
- Transfers create an auditable outflow and inflow linked by one transfer transaction.
- All stock changes emit domain events.

### 13.3 Inventory event flow

```
Command
 → Authorization
 → Validate product/warehouse/quantity
 → Begin transaction
 → Write StockLedgerEntry
 → Update StockBalance projection
 → Write Outbox event
 → Commit transaction
 → Worker publishes/consumes event
 → Notifications / analytics / security processing
 → Audit trail
```

### 13.4 Inventory acceptance tests

- Receive 100 units → balance increases by exactly 100.
- Issue 20 units → balance decreases by exactly 20.
- Transfer 10 units A→B → A decreases 10 and B increases 10 atomically.
- Unauthorized adjustment → no stock mutation.
- Duplicate command/event → no double movement.
- Adjustment above threshold → required approval/security alert.
- Low-stock threshold crossed → one appropriate alert per configured deduplication window.
- Concurrent issue operations cannot corrupt balance.

## 14. Procurement Module — Detailed Development

### 14.1 Workflow

```
Employee
 → Purchase Request
 → Validation
 → Approval Workflow
   ├─ Reject → Request Closed
   └─ Approve
       → Purchase Order
       → Supplier
       → Goods Receipt
       → Inventory
       → Supplier Invoice
       → Finance Verification
       → Payment
       → Audit
```

- Purchase request must capture requester, department/branch, items, quantities, justification, estimated value and required date.
- Approval thresholds must be configurable rather than hard-coded.
- A requester must not approve their own request where segregation-of-duties policy prohibits it.
- Purchase order numbers must be unique and traceable to the request.
- Goods receipt must record who received, what was received and discrepancies.
- Invoice matching should support PO/receipt checks before payment.
- All approval decisions are audited.

### 14.2 Procurement edge cases

Partial delivery, over-delivery, under-delivery, cancelled purchase order, rejected request, supplier invoice without a matching PO, duplicate invoice number, price mismatch, currency mismatch (if multi-currency enabled later), payment attempted against an already settled invoice.

## 15. Sales, Fulfillment and Billing Module

```
Quotation
→ Customer acceptance
→ Sales Order
→ Customer/credit validation
→ Stock availability/reservation
→ Pick/Pack
→ Delivery
→ Invoice
→ Payment
→ Receipt/reconciliation
→ Reporting + audit
```

- Validate customer status before accepting orders.
- Apply pricing and tax rules consistently server-side.
- Reserve inventory where the business policy requires it.
- Do not allow a UI-only stock check to guarantee availability; enforce the rule transactionally.
- Delivery is a separate auditable state transition.
- Invoice generation must be idempotent.
- Payment creation must be idempotent and protected against duplicate callbacks/requests.
- Payment status changes must be audited.

## 16. Finance and Payment Integration

- Separate payment intent/request from payment confirmation.
- Never trust a client-provided "paid" flag.
- Validate provider callbacks using the provider's secure verification mechanism.
- Use idempotency keys and provider transaction references.
- Record raw provider response only when appropriate and never store sensitive authentication data.
- Reconcile internal invoice/payment state with provider state.
- Define explicit states: Pending, Successful, Failed, Reversed/Refunded where required.
- All financial changes require authorization and audit.

## 17. Workflow and Approval Engine

- Represent workflow definitions separately from workflow instances.
- Workflow definitions contain stages, conditions, required roles and escalation behavior.
- Workflow instances record the current state and history.
- Every transition must validate that the actor is authorized.
- Prevent duplicate approvals.
- Prevent approval after cancellation/expiry.
- Support configurable monetary thresholds.
- Record transition history for audit.
- Emit events such as ApprovalRequested, Approved and Rejected.

**Example:**
```
PurchaseAmount < threshold A → Manager
threshold A–B → Manager + Finance
above threshold B → Manager + Finance + Executive
```

## 18. Audit and Security Module

- Audit authentication events.
- Audit role/permission changes.
- Audit creation, modification and deletion of high-value records.
- Audit stock adjustments/transfers.
- Audit approvals and rejections.
- Audit invoices and payments.
- Audit exports and document access where required.
- Audit administrative configuration changes.
- Do not expose audit mutation endpoints to ordinary users.
- Provide filters by actor, action, resource, tenant, severity and time.

### 18.1 Security anomaly pipeline

```
Application/Security Event
→ Event Collector
→ Normalization
→ Detection Rule
→ Risk/Severity
→ Security Alert
→ Notification
→ Investigation
→ Response Action
→ Closure
```

- Start with deterministic rules before introducing ML.
- Each rule needs an owner, threshold, evidence fields and false-positive handling.
- Security alerts need lifecycle states: Open, Acknowledged, Investigating, Resolved, False Positive.
- Administrative response actions must be audited.

## 19. Event Bus, Outbox and Background Workers

This is a critical reliability component.

### 19.1 Outbox implementation

1. Open a database transaction.
2. Perform the business state change.
3. Create an OutboxMessage in the same transaction.
4. Commit both.
5. Background publisher reads unprocessed outbox rows.
6. Publish message to the configured bus.
7. Mark the outbox record as published.
8. Retry transient failures with controlled backoff.
9. Move repeatedly failing messages to a dead-letter/error state.
10. Make consumers idempotent using eventId/business reference keys.

### 19.2 Consumer rules

- Assume at-least-once delivery.
- Never assume a message is delivered exactly once.
- Persist processing/deduplication state where necessary.
- Keep handlers small and observable.
- Do not put long-running synchronous work inside an HTTP request when it can be asynchronous.
- Propagate tenantId, correlationId, causationId and actor information.
- Version event schemas when contracts evolve.

## 20. Notification and Real-Time Module

- Define notification templates separately from business logic.
- Use preferences to determine enabled channels.
- Use SignalR for in-app real-time events.
- Use provider adapters for email/SMS/push so business logic does not depend on one vendor.
- Persist delivery status.
- Deduplicate alerts where the same condition can fire repeatedly.
- Never let a notification provider failure roll back a completed business transaction.

## 21. Document Management Module

- Database stores document metadata; object storage stores file content.
- Generate non-guessable object keys.
- Authorize downloads through the API.
- Limit upload size and accepted types.
- Validate file content and scan where available.
- Record uploader, parent entity, timestamp and access history.
- Define retention/deletion behavior.
- Do not expose storage buckets/containers directly unless access is deliberately controlled.

## 22. React Frontend Development Standard

- Organize code by business feature, not only by technical file type.
- Use TypeScript strict mode.
- Keep server state separate from local UI state.
- Centralize API client behavior for authentication, errors, correlation IDs and retries.
- Use reusable form controls and validation patterns.
- Implement route-level authorization checks for user experience, but rely on server authorization for security.
- Every page must define loading, empty, error, success and unauthorized states.
- Use accessible labels, keyboard navigation, semantic controls and clear validation messages.
- Use responsive layouts suitable for desktop and operational tablet screens.
- Avoid duplicating business rules in React.

### 22.1 Frontend feature pattern

```
features/inventory/
  api.ts
  types.ts
  hooks.ts
  components/
  pages/
  validation.ts
  routes.tsx
```

### 22.2 Standard page states

| State | Required behavior |
|---|---|
| Loading | Show progress/skeleton without flashing invalid data. |
| Empty | Explain what is empty and provide the next permitted action. |
| Success | Show result and clear next actions. |
| Validation error | Highlight fields and provide actionable messages. |
| API error | Show safe error message and correlation/reference where appropriate. |
| 403 | Explain insufficient permission; do not expose hidden data. |
| 404 | Resource not found or no longer accessible. |
| Conflict | Tell user the record changed and offer refresh/retry. |
| Offline/network failure | Allow retry and preserve safe unsaved input where appropriate. |

## 23. API Development Standard

- Use nouns/resources for REST endpoints and explicit commands for state transitions where appropriate.
- Version APIs from the beginning.
- Use DTOs rather than exposing EF/domain entities.
- Validate request payloads.
- Return consistent error structures.
- Use pagination for collections.
- Use filtering/sorting only through an allowlisted field set.
- Use authorization policies on endpoints and/or application handlers.
- Use idempotency for retry-sensitive operations.
- Generate OpenAPI documentation.
- Document status codes and error conditions.
- Do not return internal exception messages or stack traces in production.

**Example error:**
```json
{
  "code": "INVENTORY_INSUFFICIENT",
  "message": "The requested quantity is not available.",
  "correlationId": "...",
  "details": [...]
}
```

## 24. Application/Domain Development Pattern

- **Command:** expresses an intent to change state.
- **Query:** reads data without changing state.
- **Handler/use case:** coordinates validation, authorization and domain operations.
- **Domain entity:** owns business invariants.
- **Repository/query abstraction:** isolates persistence concerns where useful.
- **Domain event:** records a meaningful state transition.
- **Integration event:** represents a message intended for another bounded component/system.
- **DTO:** defines external contract.
- **Validator:** validates input and basic business constraints before domain execution.
- **Policy:** evaluates authorization conditions.

## 25. Testing Programme

### 25.1 Unit tests
Entity invariants, approval thresholds, pricing/tax calculations, stock quantity rules, payment state transitions, permission policies, anomaly detection rules.

### 25.2 Integration tests
Real test database, EF Core mappings, transactions and concurrency, outbox persistence, API + authorization, object storage adapter, messaging adapter, provider callback verification.

### 25.3 E2E tests
Login → dashboard. Purchase request → approval → PO → receipt. Quotation → order → delivery → invoice → payment. Inventory adjustment → security alert. User creation → role assignment → restricted endpoint. Document upload → authorized download.

### 25.4 Negative/security tests
IDOR/resource enumeration, cross-tenant access, privilege escalation, broken object-level authorization, mass assignment, injection attempts, malformed payloads, rate-limit behavior, replay/duplicate payment request, duplicate event processing, expired/revoked authentication, unauthorized file access.

## 26. Security Development Lifecycle

1. Identify assets and trust boundaries.
2. Create a lightweight threat model for each sensitive module.
3. Identify abuse cases.
4. Define authorization rules before implementation.
5. Implement secure defaults.
6. Add automated security tests.
7. Run dependency scanning.
8. Run static analysis.
9. Scan container images.
10. Review secrets/configuration.
11. Perform API authorization testing.
12. Perform manual penetration testing before production.
13. Document residual risks and mitigations.

- Use OWASP ASVS as a verification checklist.
- Treat tenant isolation, payments, stock and privileged administration as high-risk areas.
- Log security events without leaking credentials or tokens.

## 27. Performance Engineering

- Establish baseline measurements before optimizing.
- Profile database queries.
- Add indexes based on actual query plans.
- Use pagination.
- Project only required columns for list/report queries.
- Cache low-volatility reference data.
- Use asynchronous processing for heavy tasks.
- Use connection pooling appropriately.
- Test concurrent stock/payment operations.
- Load-test critical endpoints before production.
- Monitor p95/p99 latency rather than only averages.

## 28. Reliability and Failure Handling

| Failure | Expected behavior |
|---|---|
| Database transient failure | Controlled retry where safe; request fails safely if transaction cannot complete. |
| Message bus unavailable | Business transaction can commit; event remains in Outbox. |
| Worker crashes | Message remains recoverable; consumer resumes idempotently. |
| Notification provider unavailable | Business transaction remains successful; notification retries. |
| Duplicate payment callback | No duplicate payment. |
| Concurrent stock issue | Transaction/concurrency rules prevent corrupted balance. |
| Object storage unavailable | Do not create an invalid document reference; retry or report failure. |
| External API timeout | Timeout + controlled retry/circuit behavior; no indefinite request blocking. |
| Bad event payload | Reject/quarantine; alert; preserve evidence for investigation. |

## 29. Observability Implementation

- Every HTTP request receives a correlation/request ID.
- Include tenant, user/actor, route, status and duration in safe structured logs.
- Track database and external dependency latency.
- Track queue depth, retry counts and dead-letter counts.
- Track business metrics: orders, payments, stock adjustments, approvals.
- Track security metrics: failed logins, blocked access, alerts.
- Create dashboards for API health, database health, worker health and business operations.
- Define alerts with actionable thresholds and owners.

## 30. Docker and Local Infrastructure

- Create reproducible development containers for database and required infrastructure.
- Use separate production images for API and worker if deployment requires independent scaling.
- Run as a non-root user where practical.
- Keep images minimal and patched.
- Use multi-stage builds.
- Inject configuration at runtime.
- Add health checks.
- Never bake secrets into images.

**Local stack:** React frontend, ASP.NET Core API, Worker, PostgreSQL/SQL Server, Redis (if enabled), Message broker/emulator (if enabled), Object storage emulator (if enabled).

## 31. CI Pipeline — Exact Gate Sequence

1. Checkout source.
2. Restore dependencies.
3. Run formatting/lint checks.
4. Run static analysis.
5. Build backend.
6. Build frontend.
7. Run unit tests.
8. Run integration tests.
9. Generate coverage report.
10. Run dependency vulnerability scan.
11. Run secret scan.
12. Build container images.
13. Scan container images.
14. Publish artifacts only if all mandatory gates pass.

A failed security gate must fail the pipeline rather than becoming an informational warning unless explicitly risk-accepted.

## 32. CD Pipeline — Staging to Production

1. Select immutable artifact/version.
2. Deploy database migration using a backward-compatible strategy.
3. Deploy API/worker.
4. Deploy frontend.
5. Run health checks.
6. Run smoke tests.
7. Run critical E2E tests.
8. Verify logs/metrics.
9. Require approval for protected production environment.
10. Deploy production.
11. Run post-deployment verification.
12. Monitor error rate, latency and business transaction health.
13. Declare release successful only after the defined observation window.
14. Rollback application artifact or execute documented forward-fix strategy depending on migration compatibility.

## 33. Azure Production Preparation

- Choose managed relational database tier appropriate to expected load.
- Use managed application hosting or containers.
- Use object storage for documents.
- Use managed cache where required.
- Use managed messaging for asynchronous events.
- Use centralized monitoring/logging.
- Use managed secret storage.
- Configure TLS, DNS, allowed origins and network access.
- Enable backups and retention.
- Define disaster recovery objectives.
- Restrict management access.
- Apply least privilege to managed identities/service principals.

The exact Azure service selection should be recorded as an Architecture Decision Record (ADR) because cost, scale and operational requirements may change.

## 34. Environment Strategy

| Environment | Purpose | Data |
|---|---|---|
| Local | Developer work | Synthetic only |
| CI/Test | Automated verification | Generated test data |
| Staging | Production-like validation | Synthetic/anonymized |
| Production | Real operations | Real data; strict access |

- Never copy production data to developer machines unless an approved, sanitized process exists.
- Use separate credentials, databases and storage for every environment.
- Production configuration must not be reused locally.

## 35. Release Readiness Checklist

| Gate | Pass criteria |
|---|---|
| Functional | All acceptance criteria passed. |
| API | Contracts documented; validation and authorization tested. |
| Database | Migration tested; indexes/constraints reviewed. |
| Security | No unresolved critical/high security defects; security tests pass. |
| Tests | Required unit/integration/E2E suites pass. |
| Performance | Critical workloads meet agreed targets. |
| Reliability | Retry, idempotency, outbox and failure paths tested. |
| Observability | Logs, metrics, alerts and health checks verified. |
| Backup | Backup exists and restore procedure tested. |
| Deployment | Staging deployment successful and repeatable. |
| Documentation | User/admin/developer/runbook documentation updated. |
| Rollback | Rollback or forward-fix strategy documented. |
| Approval | Technical + business release approval obtained. |

## 36. Production Deployment Procedure

1. Announce release window and responsible engineer.
2. Confirm backups and database health.
3. Confirm artifact version and checksum/tag.
4. Confirm environment configuration and secrets.
5. Confirm database migration compatibility.
6. Deploy infrastructure/configuration changes first where required.
7. Apply database migration.
8. Deploy backend/worker.
9. Deploy frontend.
10. Run health checks.
11. Run smoke tests.
12. Verify authentication and one critical business journey.
13. Verify event processing and notification queues.
14. Verify monitoring dashboards.
15. Monitor for errors and abnormal business activity.
16. Communicate release completion.
17. Record release version, time, changes and any deviations.

## 37. Rollback and Incident Procedure

### 37.1 Application rollback
- Stop promotion of the faulty artifact.
- Identify last known-good version.
- Redeploy last known-good application where database compatibility permits.
- Continue monitoring.
- Open incident record and preserve logs/correlation IDs.

### 37.2 Database migration failures
- Prefer backward-compatible expand/contract migrations.
- Do not automatically execute destructive rollback in production without a verified recovery plan.
- Use restore/forward migration when required.
- Test migration recovery in staging before release.

### 37.3 Security incident
- Contain affected accounts/sessions/resources.
- Preserve evidence.
- Revoke compromised credentials/tokens.
- Review audit events and logs.
- Assess affected tenants/data.
- Apply remediation.
- Document root cause and preventive controls.

## 38. Documentation Required Before Handover

System architecture document, module specifications, API/OpenAPI documentation, database/ERD documentation, event catalogue, environment/configuration guide, local development guide, deployment guide, rollback guide, backup/restore runbook, security runbook, incident response runbook, monitoring/alert guide, user/admin guide, known limitations and technical debt register, ADR register, release notes.

## 39. Developer Daily Workflow

1. Pull latest main/develop branch.
2. Read the story/issue and acceptance criteria.
3. Identify impacted modules and security boundaries.
4. Create a short-lived branch.
5. Update design/ADR if the architecture changes.
6. Implement domain rules first.
7. Implement application use case.
8. Implement persistence/migration.
9. Implement API contract.
10. Implement events/outbox.
11. Implement frontend feature.
12. Write unit/integration/E2E tests.
13. Run local quality checks.
14. Update documentation.
15. Commit focused changes.
16. Open pull request with evidence.
17. Address review comments.
18. Merge only after CI and review gates pass.

## 40. Code Review Checklist

- Does the code satisfy the stated acceptance criteria?
- Are business rules in the appropriate backend/domain layer?
- Can an unauthorized user execute the operation by calling the API directly?
- Is tenant/branch scope enforced?
- Could a request be replayed and create a duplicate transaction?
- Are transactions correctly bounded?
- Are concurrency issues handled?
- Are events published reliably through the outbox?
- Are consumers idempotent?
- Are exceptions handled without leaking sensitive information?
- Are logs useful but safe?
- Are tests meaningful rather than merely increasing coverage?
- Does the frontend handle all states?
- Does the change introduce unnecessary coupling or complexity?
- Are migrations safe?

## 41. Module-by-Module Completion Sequence

All columns (Backend, DB, Frontend, Events, Security, Tests, Deploy) must be checked for: Identity, Tenant/Users, Master Data, Inventory, Procurement, Workflow, Sales/Fulfillment, Billing/Payments, Audit/Security, Notifications, Documents. Reporting and AI Assistant have Events/DB marked optional respectively.

## 42. Final System End-to-End Flow

```
USER
 ↓
React UI
 ↓ HTTPS
ASP.NET Core API
 ↓
Authentication + Authorization + Tenant Scope
 ↓
Application Use Case
 ↓
Domain Validation / Business Rules
 ↓
EF Core Transaction
 ├── Business Data
 ├── Audit Record
 └── Outbox Event
 ↓
COMMIT
 ↓
Event Bus
 ├── Inventory Projection
 ├── Notification Worker
 ├── Security/Anomaly Worker
 ├── Analytics/Reporting
 └── Integration Adapter
 ↓
SignalR / Email / Other Notifications
 ↓
Dashboard / Reports / Audit
 ↓
Monitoring + Metrics + Traces
```

This is the central development principle: critical state changes are transactional; secondary reactions are event-driven; security and audit controls surround the entire lifecycle.

## 43. AI Assistant Development Rules

- Release AI only after deterministic reporting and authorization are reliable.
- AI receives only data the requesting user is already permitted to see.
- Use predefined tools/query handlers instead of unrestricted database access.
- Validate tool inputs and enforce tenant scope in every tool.
- Treat AI output as an explanation of retrieved data, not as an authoritative transaction.
- Keep the first release read-only.
- Log AI requests and tool usage without storing unnecessary sensitive prompts/data.
- Add evaluation cases for incorrect, ambiguous and unauthorized questions.

## 44. Portfolio Demonstration Script

1. Log in as an administrator and show roles/permissions.
2. Create a branch, warehouse, supplier, customer and product.
3. Log in as an employee and create a purchase request.
4. Show approval routing and segregation-of-duties behavior.
5. Approve the request as a manager.
6. Generate a purchase order.
7. Receive goods and show the stock ledger changing.
8. Create a customer sales order.
9. Reserve/issue stock and complete delivery.
10. Generate an invoice and record a payment.
11. Show audit history linking actor → action → resource → timestamp.
12. Trigger a controlled high-risk stock adjustment and show the security alert.
13. Show real-time notification in the React interface.
14. Open management reports.
15. Ask the read-only AI assistant an authorized operational question.
16. Show CI pipeline and automated tests.
17. Show Docker deployment and Azure monitoring.

## 45. Project Management and Tracking

- Create epics for each major module.
- Break epics into vertical user stories that cross backend/database/frontend rather than isolated technical tasks.
- Each story must have acceptance criteria and test cases.
- Maintain a dependency map so foundational work is completed first.
- Maintain a technical debt register.
- Maintain an ADR whenever a decision changes architecture, data model, security posture or major infrastructure.
- Tag releases with semantic versioning or the team's selected version scheme.
- Keep a changelog.

| Work item template | Required contents |
|---|---|
| Story | User, goal, value, acceptance criteria |
| Technical task | Implementation detail, affected components, completion criteria |
| Bug | Observed behavior, expected behavior, reproduction, impact, evidence |
| Security issue | Asset, threat, exploitability, impact, mitigation, verification |
| ADR | Context, decision, alternatives, consequences |
| Release | Version, scope, migrations, risks, rollback, approvals |

## 46. Recommended Development Milestones

| Milestone | Exit condition |
|---|---|
| M0 Architecture approved | Architecture, domain boundaries and security principles documented. |
| M1 Skeleton | Repo builds; API, React, DB, CI and local environment work. |
| M2 Secure identity | Authentication, tenant isolation and authorization tested. |
| M3 Inventory complete | Ledger, balances, concurrency and alerts work. |
| M4 Procure-to-receive | Request → approval → PO → receipt works. |
| M5 Order-to-cash | Order → fulfillment → invoice → payment works. |
| M6 Enterprise controls | Audit, security detection, notifications and documents work. |
| M7 Reporting | Operational dashboards and reports work. |
| M8 Hardening | Security, performance, resilience and E2E gates pass. |
| M9 Production | Azure deployment, monitoring, backup/restore and release procedure verified. |
| M10 Handover | Documentation, runbooks, backlog and support ownership complete. |

## 47. Common Mistakes to Avoid

Building a large UI before designing the domain; putting authorization only in React; using database updates instead of stock ledger transactions; publishing events directly from controller code; assuming message delivery is exactly once; using floating point for money; hard-coding approval thresholds; allowing users to approve their own requests; returning EF entities directly from APIs; storing secrets in Git; logging tokens/passwords; using production data for development; skipping integration tests because unit tests pass; skipping migration testing; treating audit logs as ordinary editable records; making the AI assistant capable of unrestricted SQL or unauthorized actions; splitting into microservices before there is a real reason; deploying without monitoring or rollback planning.

## 48. Final Engineering Checklist

Requirements and acceptance criteria complete; Architecture and ADRs updated; Domain rules implemented and tested; Database migration reviewed and tested; Tenant isolation verified; Authentication and authorization verified; API contracts documented; Frontend states implemented; Events and outbox implemented; Consumers idempotent; Audit coverage complete; Security tests complete; Dependency/secret/container scans complete; Performance tests complete for critical paths; Health checks and observability complete; Backup/restore verified; CI pipeline green; Staging deployment successful; Smoke/E2E tests successful; Production approval recorded; Production deployment successful; Post-deployment checks successful; Runbooks updated; Release notes written; Known limitations recorded; Support ownership assigned; Future backlog prioritized.

## 49. Reference Standards and Documentation

| Reference | Use |
|---|---|
| Microsoft Azure Well-Architected Framework | Reliability, security, performance, cost and operations design. |
| Microsoft Azure Architecture Center | Architecture patterns and cloud design. |
| ASP.NET Core documentation | API, middleware, authentication, authorization and hosting. |
| React documentation | Component and frontend architecture. |
| OWASP ASVS | Application security verification baseline. |
| OWASP Top 10 | Common application security risks and awareness. |
| OpenAPI Specification | API contract/documentation. |
| EF Core documentation | ORM, migrations, transactions and data access. |
| Docker documentation | Container build and runtime practices. |

These references should be consulted during implementation and release reviews; exact service versions and cloud configurations should be pinned to the project baseline and periodically reviewed for supported releases.

## 50. End-of-Development Handover

1. Freeze the production release branch/tag.
2. Export or publish the final API specification.
3. Publish architecture and database documentation.
4. Publish event catalogue and integration contracts.
5. Publish deployment and rollback procedures.
6. Verify monitoring dashboards and alert ownership.
7. Verify backup schedule and restore instructions.
8. Transfer required secrets/access through approved secure channels.
9. Review open defects and technical debt.
10. Review support procedures and escalation contacts.
11. Conduct final demonstration.
12. Obtain business acceptance.
13. Record final release version and deployment date.
14. Move remaining work into the prioritized backlog.
15. Begin the operations/maintenance lifecycle.

## 51. Final Principle

The objective is not simply to finish an application. The objective is to produce a system that another engineer can build, test, deploy, operate, audit, secure and extend without depending on undocumented knowledge. Every important behavior must therefore have a clear owner, a defined data model, an API or command contract, an authorization rule, an event/audit consequence where applicable, automated tests and an operational procedure.

# Enterprise Business Operations & Security Platform — Architecture Overview

## Architecture
C# / ASP.NET Core + React / TypeScript + PostgreSQL or SQL Server + Azure

## Core modules
- Identity & Access
- Tenant / Branch / Warehouse
- Inventory
- Procurement
- Sales
- Finance / Billing / Payments
- Workflow & Approvals
- Security & Audit
- Notifications
- Documents
- Reporting / Analytics
- Optional AI Assistant

## Event flow
`Command -> Domain Transaction -> Domain Event -> Outbox -> Event Bus -> Workers -> Notifications / Analytics / Security / Audit`

## Critical invariants
- Tenant isolation
- Server-side authorization
- Append-only stock ledger
- Auditable high-value actions
- Idempotent payment/event handling
- No secrets in source control
- All critical APIs tested
- Production deployment monitored

## Full specification
The complete system design (DFDs, ERD, event flows, deployment, CI/CD) and the execution playbook live in [`../source-specs/`](../source-specs/):
- [`01-system-design-specification.md`](../source-specs/01-system-design-specification.md) — what to build
- [`02-development-guide.md`](../source-specs/02-development-guide.md) — how to build it, in order

See also [`configuration.md`](configuration.md) for the configuration/secrets strategy,
[`idempotency.md`](idempotency.md) for the idempotency convention, and [`../decisions/`](../decisions/)
for ADRs recording choices not fixed by the spec (database engine, etc).

## Sources
- Microsoft Azure Architecture Center
- Microsoft Azure Well-Architected Framework
- OWASP ASVS
- React documentation
- ASP.NET Core documentation

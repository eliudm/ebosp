# ADR-0001: Start as a modular monolith, not microservices

- **Status:** Accepted
- **Date:** 2026-08-30

## Context
EBOSP spans many business domains (identity, inventory, procurement, sales, finance, workflow, security, notifications, documents, reporting, AI). A distributed microservice estate adds deployment, networking, data-consistency and operational complexity that isn't justified before the system has real users or scale requirements. Per the system design spec (§1, §32) and the development guide (§1, §4), the project explicitly targets a modular monolith for Release 1.

## Decision
Build EBOSP as a single ASP.NET Core solution organized into strict layers (`EBOSP.Domain`, `EBOSP.Application`, `EBOSP.Infrastructure`, `EBOSP.Api`, `EBOSP.Worker`, `EBOSP.Contracts`) with clear module boundaries inside `Domain`/`Application` (Identity, Inventory, Procurement, Sales, Finance, Workflow, Security, Notifications, Documents, Reporting). Communication between modules happens through in-process application services and domain events published via the Outbox pattern — never direct cross-module database access. This keeps the option to extract a module into its own service later without a rewrite, if a concrete scalability or organizational need arises.

## Alternatives Considered
- **Microservices from day one** — rejected: massive premature operational overhead (service discovery, distributed transactions, multi-repo CI/CD) for a system with no proven scale requirement. Explicitly called out as an overengineering risk in the spec (§33).
- **Single unstructured monolith (no module boundaries)** — rejected: would make later extraction impossible and encourage tangled cross-cutting business rules, contradicting the Domain/Application layering standard (dev guide §5, §24).

## Consequences
- Faster initial delivery, one deployable unit, one CI/CD pipeline, simpler local dev.
- Module boundaries must be actively enforced in code review (dev guide §40) since nothing physically prevents a cross-module shortcut.
- Extraction into a separate service is deferred until a real trigger exists (scale, team ownership split, compliance isolation) and would itself require a new ADR.

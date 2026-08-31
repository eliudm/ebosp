# EBOSP — Enterprise Business Operations & Security Platform

A production-style, modular business application combining operational workflows (inventory, procurement, sales, invoicing, payments) with security, auditability, reporting, real-time notifications, anomaly detection, document management and an optional read-only AI assistant.

**Stack:** C# / ASP.NET Core · React / TypeScript · PostgreSQL or SQL Server · Azure · Docker · CI/CD
**Architecture:** Modular monolith + asynchronous domain events (Outbox pattern) + REST API + relational database. See [ADR-0001](docs/decisions/0001-modular-monolith-first.md).

## Source of truth

This repository is built strictly against two governing documents, transcribed in full into `docs/source-specs/`:

| Document | Purpose |
|---|---|
| [`docs/source-specs/01-system-design-specification.md`](docs/source-specs/01-system-design-specification.md) | **What** to build — scope, domain model, event catalogue, API spec, security/authorization model, NFRs, acceptance criteria. |
| [`docs/source-specs/02-development-guide.md`](docs/source-specs/02-development-guide.md) | **How** to build it — Definition of Done, exact build order, module-by-module implementation detail, testing programme, CI/CD gates, release procedure. |

Do not deviate from these without recording an ADR in `docs/decisions/`. See also [`docs/architecture/overview.md`](docs/architecture/overview.md) for a one-page summary.

## Repository structure

```
src/                  ASP.NET Core solution (Api, Application, Domain, Infrastructure, Contracts, Worker)
frontend/             React + TypeScript app (feature-organized)
tests/                UnitTests, IntegrationTests, ApiTests, E2ETests
docs/                 architecture, api, modules, runbooks, decisions (ADRs), source-specs
infra/                docker, azure
scripts/              dev/CI convenience scripts
.github/workflows/    CI/CD pipelines
```

Full structure rationale: dev guide §5.

## Build order (do not skip ahead)

Per dev guide §4, modules are built in dependency order — each phase assumes the previous one is done and tested:

`0` Requirements/architecture (this prep) → `1` Repo/CI/DB foundation → `2` Identity & tenant → `3` Master data (branches/warehouses/products) → `4` Inventory ledger → `5` Procurement + approvals → `6` Sales + fulfillment → `7` Billing + payments → `8` Audit + security analytics → `9` Notifications + documents → `10` Reporting → `11` AI assistant (read-only) → `12` Hardening → `13` Azure deployment → `14` Handover.

## Progress tracker

Update this table at the end of every work session so the next session (or a fresh context) can pick up exactly where we left off. Status values: `Not started` / `In progress` / `Done`.

| Milestone | Exit condition (dev guide §46) | Status |
|---|---|---|
| M0 Architecture approved | Architecture, domain boundaries and security principles documented | **Done** — this prep pass |
| M1 Skeleton | Repo builds; API, React, DB, CI and local environment work | **Done** — solution/frontend build, tests pass, local Postgres via Docker verified end-to-end (migrations apply, API starts, `/health` returns 200), production Dockerfiles built/scanned clean, CI pipeline green on all 6 gates (`.github/workflows/ci.yml`). |
| M2 Secure identity | Authentication, tenant isolation and authorization tested | Not started |
| M3 Inventory complete | Ledger, balances, concurrency and alerts work | Not started |
| M4 Procure-to-receive | Request → approval → PO → receipt works | Not started |
| M5 Order-to-cash | Order → fulfillment → invoice → payment works | Not started |
| M6 Enterprise controls | Audit, security detection, notifications and documents work | Not started |
| M7 Reporting | Operational dashboards and reports work | Not started |
| M8 Hardening | Security, performance, resilience and E2E gates pass | Not started |
| M9 Production | Azure deployment, monitoring, backup/restore and release procedure verified | Not started |
| M10 Handover | Documentation, runbooks, backlog and support ownership complete | Not started |

**Current state:** Phase 1 (Foundation) complete. .NET solution (Api, Application, Domain, Infrastructure, Contracts, Worker + 4 test projects) builds and all tests pass; React/Vite frontend builds. PostgreSQL runs locally via Docker Compose (ADR-0002); EF Core migrations apply cleanly. Foundation module cross-cutting infrastructure (dev guide §9) is in place: global exception handling (ProblemDetails), correlation ID middleware, structured JSON logging, health checks, current-user/tenant context abstraction, clock abstraction, unit-of-work abstraction, pagination/sort conventions, authorization policy registration point, and a documented (not-yet-built, no consumer yet) idempotency convention. API versioning strategy recorded in ADR-0003. Verified end-to-end: `dotnet ef database update` + `dotnet run` + `GET /health` → 200.

Production Dockerfiles for the API and Worker are done (`infra/docker/api.Dockerfile`, `worker.Dockerfile`) — multi-stage, non-root, runtime-injected config, container health checks — verified by building and running both images against the local Postgres container, and by a clean Trivy scan (0 vulnerabilities after pinning `apk upgrade` in the runtime stage).

CI pipeline (`.github/workflows/ci.yml`) is live and green, following the dev guide §31 gate sequence: backend (restore/format/build-with-analyzers/test+coverage/dependency scan) and frontend (lint/typecheck/build/audit) in parallel, then a secret scan (gitleaks), then container build + Trivy scan for both images, then a publish-artifacts gate that only runs if everything upstream passed. Runs on push/PR to `main`.

Dev guide §7 step 12 (start API and frontend, smoke-test) is verified for both: API via `dotnet ef database update` + `dotnet run` + `GET /health` → 200 (above), frontend via `npm run dev` serving the app shell and transforming `main.tsx` cleanly with no runtime errors.

Git workflow deliberately deviates from dev guide §6 (protected `main` + feature branches + PRs) for the current solo-development phase - see [ADR-0004](docs/decisions/0004-trunk-based-solo-workflow.md). `main` is unprotected; CI on every push is the actual quality gate.

**Not yet done:** the smoke-test checklist automation (dev guide §7 step 12) and the CD pipeline (§32, Phase 13/Azure). No business feature/domain code has been written — that starts with Identity in Phase 2, per the build order.

## Development discipline (non-negotiable, dev guide §1)

- Do not build screens before domain rules, entities, workflows, API contracts and acceptance criteria.
- Do not trust the frontend for security — every authorization/validation rule is enforced server-side.
- Do not mutate stock, payments or other high-value state through ad-hoc updates — only explicit, audited business commands.
- Do not publish an event before its DB transaction is committed — use the Outbox pattern.
- A feature is not "done" until it satisfies the full [Definition of Done](docs/source-specs/02-development-guide.md#2-definition-of-done--mandatory-for-every-feature) (requirement, design, backend, database, frontend, events, audit, security, tests, observability, docs, review, deployment, acceptance).

## Getting started (once Phase 1 lands)

Local dev setup steps are specified in dev guide §7 — will be reflected here with real commands once the .NET solution and React app exist.

## License

TBD.

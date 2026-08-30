# ADR-0002: Use PostgreSQL as the relational database

- **Status:** Accepted
- **Date:** 2026-08-30

## Context
The system design spec (§0 technology target, §13) allows either PostgreSQL or SQL Server, both supported by EF Core with strong transactional/constraint guarantees. A single choice needs to be pinned so migrations, Docker Compose, connection-string configuration and the Azure target are consistent across the project.

## Decision
Use **PostgreSQL** (via the `Npgsql.EntityFrameworkCore.PostgreSQL` provider) as the relational database for all environments: local (Docker), CI/test, staging and production (Azure Database for PostgreSQL Flexible Server).

## Alternatives Considered
- **SQL Server** — rejected for this project: higher Azure cost at small/portfolio scale, and licensing considerations for local dev outside Docker. Still a fully valid choice per the spec if organizational requirements change.

## Consequences
- `infra/docker/docker-compose.yml` runs a local `postgres` container for development.
- EF Core migrations, connection strings and any raw SQL/tooling target Postgres syntax.
- Azure Database for PostgreSQL Flexible Server is the target in `infra/azure/` (ADR to be written when Azure provisioning is scoped in Phase 13).

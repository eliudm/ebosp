# Docker

Local development infrastructure. Currently: PostgreSQL only (ADR-0002). Redis, a message broker and an object-storage emulator will be added here if/when a concrete need arises (dev guide §30 lists them as "if enabled" — the Outbox pattern initially uses the same Postgres database, so no separate broker is required to start).

## Usage

From the repo root:

```
docker compose -f infra/docker/docker-compose.yml up -d
docker compose -f infra/docker/docker-compose.yml down
```

Or use the wrapper scripts in `../../scripts/` (`dev-up`, `dev-down`).

Copy `.env.example` to `.env` in this folder to override the default dev credentials (defaults are fine for local-only use; never reuse them anywhere else).

**Note:** the host port defaults to `5435`, not the standard `5432`, because this machine already runs other projects' Postgres containers on `5432` and `5433`. Override `POSTGRES_PORT` in `.env` if that's not the case on your machine.

Production Dockerfiles for the API and Worker (multi-stage, non-root, health checks, no baked-in secrets) will be added here in Phase 1 once the .NET solution exists.

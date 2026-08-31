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

## Building the production images

Build context must be the repo root (the Dockerfiles need sibling project references):

```
docker build -f infra/docker/api.Dockerfile -t ebosp-api .
docker build -f infra/docker/worker.Dockerfile -t ebosp-worker .
```

Both are multi-stage (SDK build stage, `aspnet-alpine` runtime stage), run as the images' built-in
non-root `app` user, take all configuration via environment variables at runtime (nothing baked
in), and carry a container `HEALTHCHECK`:

- API: `GET /health`.
- Worker: freshness of a heartbeat file (`$HEARTBEAT_FILE_PATH`, default `/tmp/worker-healthy`) -
  the worker has no HTTP endpoint of its own, so `HeartbeatBackgroundService` writes it every 10s.

Neither image is pushed anywhere yet - that lands with the CI/CD pipeline (blocked on the GitHub
token's `workflow` scope, see the root README).

# Configuration and Secrets Strategy

Per dev guide §8. This document is the single reference for how configuration is structured across environments; update it whenever a new configuration category is introduced.

## Hierarchy

`safe defaults` → `environment-specific configuration` → `deployment secrets`

- Defaults live in source (e.g. `appsettings.json`) and must be safe to commit — no real credentials, no production URLs.
- Environment-specific overrides (`appsettings.Development.json`, `appsettings.Staging.json`, `appsettings.Production.json`) are **not** committed except where they contain no secrets; `appsettings.Development.json` and `appsettings.Local.json` are gitignored so each developer can hold local-only values.
- Local development secrets are supplied via environment variables or the .NET user-secrets store — never committed.
- Production secrets are supplied via Azure Key Vault / managed secret storage, injected at runtime — never baked into images or committed anywhere.

## Rules

- Configuration is strongly typed and validated (`IOptions<T>` + data annotations or a validation step) at application startup.
- The application fails fast if mandatory production configuration is absent — no silent defaults for things like connection strings or signing keys in production.
- Never log connection strings, authorization headers, refresh tokens, or credentials.
- Every environment (local, CI/test, staging, production) uses separate credentials, databases and storage — never share or reuse.

## Configuration categories (dev guide §8)

Database, Identity, JWT/session, Storage, Messaging, Email/SMS providers, Payment provider, Allowed origins, Rate limits, Feature flags, Telemetry, AI provider.

## Local development

- Postgres connection: see `infra/docker/docker-compose.yml` and `infra/docker/.env.example` (ADR-0002).
- Copy `infra/docker/.env.example` → `infra/docker/.env` for local Docker credentials.
- Backend local secrets: to be documented once `EBOSP.Api` exists (Phase 1) — will use .NET user-secrets, not `appsettings.Development.json`, for anything sensitive.
- Frontend: `frontend/.env.local` (gitignored) for any local-only Vite env vars (`VITE_*`), once needed.

Not yet started: actual `appsettings.json` schema, user-secrets setup, and Key Vault wiring — these land with `EBOSP.Api` in Phase 1.

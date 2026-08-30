# ADR-0003: URL path-based API versioning

- **Status:** Accepted
- **Date:** 2026-08-30

## Context
Dev guide §9 (foundation module) requires an API versioning strategy to be settled before business
endpoints are added, so every controller follows the same convention from the first one written.

## Decision
Version the API by URL path segment: `/api/v{n}/...` (e.g. `/api/v1/products`). Every controller
inherits from a shared `ApiControllerBase` (in `EBOSP.Api`) that carries the `[Route("api/v{version:apiVersion}/[controller]")]`
convention; a version increments only on a breaking change to a given resource, not on every
release.

## Alternatives Considered
- **Header-based versioning** (`X-Api-Version`) — rejected: less discoverable/debuggable than a
  visible URL segment, harder to test with a browser or curl during development.
- **Query-string versioning** (`?api-version=1`) — rejected: easy to omit accidentally, doesn't
  play well with caching/URL-based routing conventions.
- **No versioning (evolve in place)** — rejected: the spec calls for stable contracts for external
  consumers (dev guide §on API design); breaking changes need an explicit escape hatch.

## Consequences
- Every future controller must inherit `ApiControllerBase` and live under `api/v{n}/...`.
- The `Asp.Versioning.*` NuGet packages will be added once the first real controller (Phase 2,
  Identity) needs version-aware routing; until then the route template convention alone is
  documented so nobody free-hands a route.

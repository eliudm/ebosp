# Security threat model and residual risk register

Consolidates dev guide §26's Security Development Lifecycle steps 1–3 (assets/trust boundaries, a
per-module threat model, abuse cases) and step 13 ("document residual risks and mitigations") into
one artifact. This is a synthesis, not new discovery: every module below was already put through an
independent, focused hardening review at the end of its own build phase (see README.md's
"Hardening pass on M*" paragraphs for the full detail and the actual bugs each one found and fixed).
This document is the single place that summarizes what those reviews collectively established,
what abuse cases were deliberately considered, and — just as importantly — what is genuinely **not**
covered yet and needs a human before this system goes anywhere near production traffic.

## 1. Assets and trust boundaries

| Asset | Sensitivity | Where it lives |
|---|---|---|
| Tenant business data (orders, invoices, stock, suppliers, customers) | High — the core product | Postgres, behind the global `ITenantOwned` query filter |
| Credentials (password hashes, refresh tokens, JWT signing key) | Critical | Postgres (hashes/tokens), Key Vault/user-secrets/env (signing key) |
| Audit trail (Outbox) | High — tampering here defeats every other control's accountability | Postgres, append-only, no update/delete path exposed anywhere |
| Uploaded documents | Medium–High (contents vary by what a tenant uploads) | Local disk today (`LocalDiskObjectStorage`) / object storage in production |
| AI assistant conversations | Medium (business-question content, not stored) | Never persisted — see §4's AI assistant note |
| Anthropic API key | Critical | `Ai:ApiKey` config, never logged, never in a response body |

**Trust boundaries** (where an actor could try to cross from a lower-trust to a higher-trust zone):

1. **Internet → API**: any unauthenticated request. Closed by JWT bearer auth + rate limiting on sensitive endpoints (login, password reset, tenant creation).
2. **Authenticated user (tenant A) → tenant B's data**: the single most-tested boundary in this codebase — a global EF Core query filter (`e.TenantId == currentUserContext.TenantId`) on every `ITenantOwned` entity, independently re-verified by a cross-tenant test in every module (`*IsolationTests.cs` in `EBOSP.IntegrationTests`, `*_AnotherTenants_Returns404`/`ExcludesOtherTenants` tests in `EBOSP.ApiTests`).
3. **Authenticated user (low privilege) → an action their role doesn't grant**: closed by policy-based RBAC (`[Authorize(Policy=...)]` mapped to permission-code claims baked into the JWT at login, server-signed so a client can never forge a claim it wasn't issued).
4. **The AI assistant → data the asking user isn't personally authorized for**: the newest and narrowest boundary (M11) — `unusual_stock_adjustments` wraps `audit.read`-gated data; `AssistantService` checks the caller's actual permission before even offering that tool to the model, and again before executing it if the model calls it anyway. Independently reviewed (M11 hardening pass) with no findings.
5. **The API/Worker → Postgres**: parameterized queries only (EF Core LINQ throughout — confirmed zero `FromSqlRaw`/`ExecuteSqlRaw` with any interpolated/concatenated value anywhere in `src/`), so this boundary has no injection surface by construction, proven rather than assumed by `MalformedPayloadTests` (M8 Hardening).
6. **The API → Anthropic's API**: outbound only, carries tool-result JSON (data the asking user is already authorized to see) plus the question text. The API key crosses this boundary as a header, never logged, never echoed in any error surfaced to a client (M11 hardening pass).
7. **The Worker → Postgres, with no `HttpContext`**: `NullCurrentUserContext` always resolves no tenant, forcing every Worker query to explicitly `.IgnoreQueryFilters()` plus an explicit tenant `Where` — the one place in the codebase where the "boundary" is enforced by a different mechanism than the global filter, specifically reviewed in M9's hardening pass.

## 2. Per-module threat model summary

Each of these was independently reviewed at the end of its phase; this table is the roll-up, not a
substitute for the detailed README paragraph:

| Module | Primary threat considered | Mechanism | Independently verified |
|---|---|---|---|
| Identity (M2) | Account enumeration, credential stuffing, session/token theft | Generic conflict responses, rate-limited login, constant-time-equivalent hashing on every path (including fast-fail), refresh-token-family revocation on reuse detection | Yes — found and fixed a real timing side-channel and an enumeration gap |
| Inventory (M3/M4) | Lost-update on concurrent stock changes, negative stock | Postgres `xmin` optimistic concurrency + reload-retry loop, hard-blocked negative balances | Yes — proven under genuine concurrent-HTTP-request tests |
| Procurement (M4) | Segregation-of-duties bypass (self-approval) | Explicit actor-not-equal-requester check on every approval action | Yes |
| Sales/Billing (M5/M7) | Overpayment, double-fulfillment, lost-update on split payments | `Invoice.PaidTotal` + `xmin` + reload-retry (mirrors Inventory's pattern) | Yes — found and fixed a real split-payment lost-update bug |
| Audit/Security (M8) | Tampering with the accountability trail; account-lockout race | Outbox is append-only with no exposed mutation path; `User` gained `xmin` after a review found a lockout-race that could let an attacker evade lockout by attacking concurrently | Yes — found and fixed a real concurrency bug in the very module meant to catch such things |
| Notifications/Documents (M9) | Cross-tenant notification/document leakage, path traversal in storage keys | Own-notifications-only checks by recipient id (never leaking existence via 403 vs 404), storage keys always server-generated GUIDs, never client input | Yes — cleanest review of the whole project, one low-severity config-binding gap found and fixed |
| Reporting (M10) | Aggregation-level cross-tenant leak (would blend data, not just expose a row) | Every report query independently filters by tenant, not relying on the global filter alone, confirmed via left-join and follow-up-lookup edge cases | Yes — found and fixed a real EF-translation bug (the low-stock report threw on every real call) plus one input-validation gap |
| AI Assistant (M11) | Assistant used as an indirect path to data the asking user isn't authorized for | Per-tool permission check, both at catalogue-build time and again at execution time | Yes — no findings, both directions independently traced through the code |
| Hardening (M8 Hardening, this phase) | The gaps between modules: SQL/XSS/mass-assignment on the negative-test side, a CI static-analysis gap, an unbounded external-API retry gap, a concurrent-migration race in the test suite itself | See §3/§5 below | This document + the CI run it produced |

## 3. Abuse cases considered (dev guide §25.4 / §26 step 3)

- IDOR / cross-tenant resource access — tested for every module (26 occurrences across the suite).
- Privilege escalation via a permission the caller's role doesn't grant — tested for every mutating endpoint (17 occurrences).
- Broken object-level authorization via a route id belonging to another tenant — same isolation tests above.
- Mass assignment (smuggling `status`/`id`/`tenantId` into a create request body) — `MalformedPayloadTests` (this phase).
- SQL injection — structurally not applicable (parameterized LINQ throughout), proven with a SQL-injection-shaped literal round-tripping as inert text (this phase).
- Reflected/stored XSS — not applicable (JSON-only API, no server-side HTML rendering), proven with an XSS-payload literal round-tripping as inert text (this phase).
- Replay / duplicate payment or receipt requests — idempotency-key uniqueness constraints (6 dedicated tests).
- Duplicate event/message processing — Outbox `PublishedAt` + the notification processor's pre-check-then-insert idempotency guard.
- Expired/revoked token reuse — refresh-token rotation with whole-family revocation on reuse detection (14 tests).
- Unauthorized file access — document authorization re-checked at both upload (parent-entity permission) and every read (the `Document` row's own tenant).
- Rate-limit abuse on sensitive endpoints — dedicated test against the real per-IP limiter (login).
- AI assistant used to reach data outside the asking user's own authorization — see §1 boundary 4.

## 4. What this changed (M8 Hardening phase)

- Closed the one negative-test category nothing else covered: SQL-injection-shaped input, XSS-payload input, mass assignment, oversized payloads (`MalformedPayloadTests`).
- Populated `EBOSP.E2ETests` with all six of dev guide §25.3's named journeys as real, continuous, HTTP-level tests (previously a placeholder).
- Added a bounded retry to `AnthropicCompletionClient` (the only outbound external-API call in this codebase) — dev guide §28: "External API timeout → timeout + controlled retry/circuit behavior."
- Added CodeQL as a dedicated static-analysis CI job (previously analyzer-only) and aggregated the existing raw coverage XML into a real, human-readable report — dev guide §31 gates 4 and 9.
- Found and fixed a real, previously-unnoticed concurrent-migration race in the test suite's own bootstrapping (a Postgres advisory lock now serializes it) — caught only by deliberately reproducing CI's fresh-database scenario locally instead of trusting a green run against an already-migrated dev database.

## 5. Residual risks — explicitly owned, not silently dropped

These are genuinely out of scope for this phase, for the same reason this project has been honest
about every other deliberate gap (no real payment provider, no geo-IP data, no export feature):
they need something this phase doesn't have — a live deployed target, a human, or an account-level
setting only the repository owner controls.

| Residual item | Why it's deferred | Owner / when |
|---|---|---|
| **Manual penetration testing** (dev guide §26 step 12) | Explicitly manual by the guide's own wording — an autonomous agent cannot perform this | A human, before any real production deployment |
| **Load testing critical endpoints** (dev guide §27) | "Load-test critical endpoints *before production*" presumes a deployed target; manufacturing numbers against a laptop's single-container Postgres would be a hollow gate | M13 Azure deployment |
| **Production observability dashboards/alerting with owners** (dev guide §29) | Needs an actual metrics/tracing backend (Azure Monitor or equivalent) — a deployment-environment decision, not a code-only addition | M13 Azure deployment |
| **CodeQL SARIF upload is non-blocking** | This repository doesn't have GitHub "Code scanning" enabled yet (Settings → Code security; a private repo also needs GitHub Advanced Security) — a repo-owner setting, not a workflow-file concern. Analysis itself runs correctly on every CI run today; only the upload step is skipped via `continue-on-error`. | Repository owner — enable code scanning, then remove the `continue-on-error: true` line in `ci.yml` |
| Multi-Worker-instance idempotency race (`NotificationOutboxProcessor`) | Theoretical today — nothing in this repo deploys multiple Worker replicas; fixing it would mean unwrapping Postgres-specific exception details, a pattern not used elsewhere | Revisit if/when multi-replica Worker deployment is planned (M13) |
| `SecurityAlert` lifecycle has no concurrency guard between two officers | Low-stakes, well-defined last-write-wins outcome, requires `security.alert.manage` already | Documented in M8's hardening pass; not revisited |
| AI assistant summaries are not authoritative | This is dev guide §43's own explicit framing of a first release ("treat AI output as an explanation of retrieved data, not an authoritative transaction"), not a bug | N/A — accepted characteristic |
| No real payment provider / geo-IP data / export feature | Neither governing doc requires them at this stage; documented per-phase when each gap was first reached | Future phase, if ever in scope |

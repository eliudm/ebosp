# Idempotency Strategy

Per dev guide §9 (foundation module: idempotency support for retry-sensitive commands).

## Convention

Any command that mutates high-value state and could plausibly be retried by a client (network
timeout, double-click, message redelivery) accepts an `Idempotency-Key` request header. The first
request with a given key executes normally and its response is stored; a repeat request with the
same key returns the stored response without re-executing the command.

## Why not built yet

No mutating command exists yet - the foundation module has no business command to make idempotent.
Building the store and middleware now would be infrastructure with no caller, so it is deferred to
the first retry-sensitive command (expected in Procurement/approvals, Phase 5, or Billing/payments,
Phase 7 - either could land first depending on how the build order plays out).

## Planned shape

- A dedicated table (e.g. `idempotency_keys`: key, tenant id, request hash, response status/body,
  created at) alongside the outbox table.
- An action filter/middleware on `ApiControllerBase`-derived commands that checks the table before
  the handler runs and writes the response after, in the same transaction as the business change.
- Keys expire after a bounded window (proposed: 24h) rather than being retained forever.

Record the concrete implementation as an ADR when the first consuming command is built, since the
storage/transaction shape may be informed by that command's specifics.

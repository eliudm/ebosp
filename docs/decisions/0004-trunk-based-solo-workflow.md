# ADR-0004: Trunk-based workflow (direct-to-main) for the solo development phase

- **Status:** Accepted
- **Date:** 2026-08-31

## Context
Dev guide §6 specifies a protected `main`, `feature/*`/`fix/*` branches, and pull requests that
require CI to pass and a review before merge. This project currently has a single contributor and
the explicit goal of frequent, small, directly-committed changes rather than batched PRs. GitHub's
own contributor count for the repo already reflects one contributor (`eliudm`); a PR-review gate
has no second reviewer to satisfy it.

## Decision
For the solo-development phase, commit directly to `main`. CI (`.github/workflows/ci.yml`)
substitutes for the pull-request gate: it runs on every push to `main` and is checked before
continuing work - as happened this session, where two real CI failures (a bad action version pin,
a genuine HIGH-severity CVE in a base image) were caught and fixed before moving on. `main` is not
branch-protected, since GitHub branch protection only meaningfully gates *merges into* a branch via
a PR, not direct pushes by the sole contributor with push access - protecting it would either block
this workflow entirely or be protection in name only.

## Alternatives Considered
- **Feature branches + self-reviewed PRs (§6 as written)** — rejected for now: a PR with no second
  reviewer adds process overhead (branch, push, open PR, self-merge) without the review benefit the
  process exists for, and directly conflicts with the project owner's explicit preference for many
  small direct commits.
- **Protect `main` and require status checks** — rejected: with a single contributor and no PR
  flow, this either blocks all direct pushes (breaking the intended workflow) or is bypassed via
  admin override on every push (protection in name only, adding friction with no real gate).

## Consequences
- CI passing on `main` is the actual quality gate; a red run on `main` must be fixed before further
  work continues, not left standing.
- This ADR should be revisited - switching to protected `main` + feature branches + PRs - once
  either: (a) a second contributor joins, or (b) the project approaches M8 Hardening / M9
  Production, where §6's review rationale becomes load-bearing rather than ceremonial.
- Commit messages for this project should not carry a `Co-Authored-By` trailer for the assistant,
  so the repository's contributor list accurately reflects the single human contributor.

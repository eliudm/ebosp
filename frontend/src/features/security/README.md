# Security feature

Audit trail views and security alert investigation screens, backed by `AuditEventsController` and
`SecurityAlertsController`. `SecurityLayout.tsx` renders two tabs: Audit Events, Security Alerts.

**`audit.read` and `security.alert.manage` are disjoint permissions** - the seed `auditor` role
holds only the former, `security-officer` only the latter, and only `tenant-admin` holds both. Both
tabs are always shown regardless of the current user's role; a user lacking the relevant permission
sees a real 403 error message on that tab's list load rather than a silently empty table.

Audit Events supports filtering by `eventType` (free text) plus the shared `DateRangeFilter`.
`actorId`/`aggregateType`/`aggregateId` are real query params on the backend but are raw GUIDs a
user can't meaningfully type into a filter box, so they're not exposed here.

Security Alerts' four lifecycle actions (Acknowledge, Investigate, Resolve, False Positive) are
always shown regardless of the alert's current status - the API has no can-transition flag, only a
runtime check that throws a real `ConflictException` message on an invalid transition. This matches
the "show the action, let the real error explain why not" pattern used for Quotation accept/reject
and Purchase Request approve/reject. Resolve and False Positive share one notes-required modal
(`notes` is `[Required]` on both, identical contract).

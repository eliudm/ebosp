# EBOSP.E2ETests

Dev guide §25.3's six critical user journeys, each a single continuous test chaining real HTTP
calls against the real Api host + real Postgres (same `WebApplicationFactory` mechanism as
`EBOSP.ApiTests`, whose helpers this project reuses via a project reference plus
`[InternalsVisibleTo]`): login -> dashboard; purchase request -> approval -> PO -> receipt;
quotation -> order -> delivery -> invoice -> payment; inventory adjustment -> security alert; user
creation -> role assignment -> restricted endpoint; document upload -> authorized download.

No browser/Playwright driver here - the frontend has no pages beyond login for any of these
journeys to click through yet (see README.md's Phase 10/11 notes), so "E2E" in this project means
the full backend request chain, not a UI automation layer. See `Journeys/`.

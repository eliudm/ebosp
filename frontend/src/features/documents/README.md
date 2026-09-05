# Documents feature

Upload, metadata, and authorized download, backed by `DocumentsController`. Documents only attach
to two entity types this phase - `PurchaseOrder` (upload needs `procurement.create`) and `Invoice`
(upload needs `invoice.create`) - via a plain `entityType`/`entityId` pair, not a generic aggregate
concept. Reads (list/get/download) need only authentication, no extra permission. There's no delete,
no versions, no draft/final status: a document is permanent immutable metadata plus a blob once
uploaded.

**No standalone `/documents` route or sidebar entry exists.** `GET /api/v1/documents` requires both
`entityType` and `entityId` - there's no "browse everything" surface, and building one would mean
asking a user to type a raw GUID into a filter box, which this app avoids everywhere else (e.g.
Security's Audit Events skips `actorId`/`aggregateId` filters for the same reason). Instead, the
shared `components/DocumentsModal.tsx` is opened from a "Documents" action on each row of
Procurement's Purchase Orders page and Sales' Invoices page - the only two places a real `entityId`
is already on screen. It has three parts: an upload form, a list (file name, size, uploaded date),
and a download button per row.

Upload permission and the allowed-extension/size-limit rules are both server config not exposed via
any endpoint, so neither is duplicated or guessed client-side - the file input accepts anything, and
the Upload button is always shown; a real 400/403 message from the backend explains a rejection
(mirrors the `*.large` permission pattern used in Inventory/Procurement/Finance). Download streams
the file's bytes directly and triggers a normal browser save via a `Blob` and a temporary
`<a download>`, reusing the file name already known from the document's list entry.

# Reports feature

Seven read-only reports (spec §8.6/§19), backed directly by `EBOSP.Api`'s `ReportsController`:
sales, inventory, low-stock, slow-moving inventory, procurement spend, outstanding invoices, top
products. `ReportsLayout.tsx` renders them as tabs under `/reports/*`. `low-stock`/`slow-moving`/
`outstanding-invoices` resolve product/warehouse/customer names client-side via `lookupApi.ts`,
since those report endpoints return raw ids.

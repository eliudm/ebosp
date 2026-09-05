# Sales feature

Customers, quotations, orders, delivery, and invoicing (dev guide §15), backed by
`CustomersController`/`QuotationsController`/`SalesOrdersController`/`DeliveriesController`/
`InvoicesController`. `SalesLayout.tsx` renders it as tabs under `/sales/*`, one per stage of the
chain: Customer → Quotation (accept/reject) → Sales Order (reserves stock) → Delivery (issues stock,
marks the order Fulfilled) → Invoice.

**Payments are out of scope here** — that's the Finance feature's job. Invoices show `Total`/`Status`
only; `PaidTotal`/`Outstanding` aren't in `InvoiceResponse` today.

**Scoped out of this pass**: edit/activate/deactivate for customers (list + create only, same cut as
every other master-data-style entity this session).

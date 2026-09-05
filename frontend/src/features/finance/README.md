# Finance feature

Invoices, payments, balances, and financial summaries (dev guide §16), backed by
`PaymentsController` plus two already-built Reports endpoints reused directly:
`GET /reports/outstanding-invoices` (the only backend surface with per-invoice `PaidTotal`/
`Outstanding` - `InvoiceResponse` itself doesn't carry them) and `GET /reports/sales` (date-ranged
`TotalInvoiced`/`TotalCollected`/`TotalOutstanding`). `FinanceLayout.tsx` renders three tabs:
Outstanding Invoices, Payments, Summary.

Payments support partial/split payments against one invoice (confirmed server-side: `PaidTotal`
accumulates until it equals `Total`, then the invoice flips to `Paid`). Confirming a payment above
`BillingOptions.LargePaymentThreshold` needs `payment.create.large`, held only by `tenant-admin` -
the `finance` role holds base `payment.create` but not the large tier.

**Scoped out of this pass**: no AR-aging or per-customer balance view exists anywhere in the backend
today, so this doesn't invent one - "balances" here means the real per-invoice `Outstanding` figure
the Reports module already computes.

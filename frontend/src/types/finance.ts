// Mirrors src/EBOSP.Contracts/Billing/{CreatePaymentRequest,PaymentResponse}.cs.
// OutstandingInvoiceItem (used throughout this feature) already lives in types/reporting.ts - the
// Reports module built it first, and it's the only backend surface that exposes per-invoice
// PaidTotal/Outstanding, so Finance reuses it rather than redefining it.

export interface CreatePaymentRequest {
  invoiceId: string
  amount: number
  method: string
  idempotencyKey?: string
}

export interface PaymentResponse {
  id: string
  invoiceId: string
  amount: number
  method: string
  status: 'Pending' | 'Successful' | 'Failed'
  createdByUserId: string
  createdAt: string
  confirmedByUserId: string | null
  confirmedAt: string | null
}

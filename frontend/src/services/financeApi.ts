// Mirrors src/EBOSP.Api/Controllers/PaymentsController.cs. getOutstandingInvoices/getSalesReport
// are re-exported from reportsApi.ts rather than duplicated - they're the same two report calls
// the Reports module already built, reused here because they're the only backend surfaces that
// expose per-invoice balances and date-ranged financial totals.

import { apiRequest } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type { CreatePaymentRequest, PaymentResponse } from '../types/finance'

export { getOutstandingInvoices, getSalesReport } from './reportsApi'

function pagedQuery(request: PagedRequest = {}): string {
  const search = new URLSearchParams()
  if (request.page !== undefined) search.set('page', String(request.page))
  if (request.pageSize !== undefined) search.set('pageSize', String(request.pageSize))
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function listPayments(request: PagedRequest = {}): Promise<PagedResult<PaymentResponse>> {
  return apiRequest<PagedResult<PaymentResponse>>(`/api/v1/payments${pagedQuery(request)}`)
}

export function createPayment(request: CreatePaymentRequest): Promise<PaymentResponse> {
  return apiRequest<PaymentResponse>('/api/v1/payments', { method: 'POST', body: request })
}

export function confirmPayment(id: string): Promise<PaymentResponse> {
  return apiRequest<PaymentResponse>(`/api/v1/payments/${id}/confirm`, { method: 'POST' })
}

export function failPayment(id: string): Promise<PaymentResponse> {
  return apiRequest<PaymentResponse>(`/api/v1/payments/${id}/fail`, { method: 'POST' })
}

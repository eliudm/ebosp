// Mirrors src/EBOSP.Api/Controllers/{Customers,Quotations,SalesOrders,Deliveries,Invoices}Controller.cs.

import { apiRequest } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type {
  CreateCustomerRequest,
  CreateDeliveryRequest,
  CreateInvoiceRequest,
  CreateQuotationRequest,
  CreateSalesOrderRequest,
  CustomerResponse,
  DeliveryResponse,
  InvoiceResponse,
  QuotationResponse,
  SalesOrderResponse,
} from '../types/sales'

function pagedQuery(request: PagedRequest = {}): string {
  const search = new URLSearchParams()
  if (request.page !== undefined) search.set('page', String(request.page))
  if (request.pageSize !== undefined) search.set('pageSize', String(request.pageSize))
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function listCustomers(request: PagedRequest = {}): Promise<PagedResult<CustomerResponse>> {
  return apiRequest<PagedResult<CustomerResponse>>(`/api/v1/customers${pagedQuery(request)}`)
}

export function createCustomer(request: CreateCustomerRequest): Promise<CustomerResponse> {
  return apiRequest<CustomerResponse>('/api/v1/customers', { method: 'POST', body: request })
}

export function listQuotations(request: PagedRequest = {}): Promise<PagedResult<QuotationResponse>> {
  return apiRequest<PagedResult<QuotationResponse>>(`/api/v1/quotations${pagedQuery(request)}`)
}

export function createQuotation(request: CreateQuotationRequest): Promise<QuotationResponse> {
  return apiRequest<QuotationResponse>('/api/v1/quotations', { method: 'POST', body: request })
}

export function acceptQuotation(id: string): Promise<QuotationResponse> {
  return apiRequest<QuotationResponse>(`/api/v1/quotations/${id}/accept`, { method: 'POST' })
}

export function rejectQuotation(id: string): Promise<QuotationResponse> {
  return apiRequest<QuotationResponse>(`/api/v1/quotations/${id}/reject`, { method: 'POST' })
}

export function listSalesOrders(request: PagedRequest = {}): Promise<PagedResult<SalesOrderResponse>> {
  return apiRequest<PagedResult<SalesOrderResponse>>(`/api/v1/sales-orders${pagedQuery(request)}`)
}

export function createSalesOrder(request: CreateSalesOrderRequest): Promise<SalesOrderResponse> {
  return apiRequest<SalesOrderResponse>('/api/v1/sales-orders', { method: 'POST', body: request })
}

export function listDeliveries(request: PagedRequest = {}): Promise<PagedResult<DeliveryResponse>> {
  return apiRequest<PagedResult<DeliveryResponse>>(`/api/v1/deliveries${pagedQuery(request)}`)
}

export function createDelivery(request: CreateDeliveryRequest): Promise<DeliveryResponse> {
  return apiRequest<DeliveryResponse>('/api/v1/deliveries', { method: 'POST', body: request })
}

export function listInvoices(request: PagedRequest = {}): Promise<PagedResult<InvoiceResponse>> {
  return apiRequest<PagedResult<InvoiceResponse>>(`/api/v1/invoices${pagedQuery(request)}`)
}

export function createInvoice(request: CreateInvoiceRequest): Promise<InvoiceResponse> {
  return apiRequest<InvoiceResponse>('/api/v1/invoices', { method: 'POST', body: request })
}

// Mirrors src/EBOSP.Api/Controllers/{Suppliers,PurchaseRequests,PurchaseOrders}Controller.cs.

import { apiRequest } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type {
  ApprovePurchaseRequestRequest,
  CreatePurchaseOrderRequest,
  CreatePurchaseRequestRequest,
  CreateSupplierRequest,
  PurchaseOrderResponse,
  PurchaseRequestResponse,
  RejectPurchaseRequestRequest,
  SupplierResponse,
} from '../types/procurement'

function pagedQuery(request: PagedRequest = {}): string {
  const search = new URLSearchParams()
  if (request.page !== undefined) search.set('page', String(request.page))
  if (request.pageSize !== undefined) search.set('pageSize', String(request.pageSize))
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function listSuppliers(request: PagedRequest = {}): Promise<PagedResult<SupplierResponse>> {
  return apiRequest<PagedResult<SupplierResponse>>(`/api/v1/suppliers${pagedQuery(request)}`)
}

export function createSupplier(request: CreateSupplierRequest): Promise<SupplierResponse> {
  return apiRequest<SupplierResponse>('/api/v1/suppliers', { method: 'POST', body: request })
}

export function listPurchaseRequests(request: PagedRequest = {}): Promise<PagedResult<PurchaseRequestResponse>> {
  return apiRequest<PagedResult<PurchaseRequestResponse>>(`/api/v1/purchase-requests${pagedQuery(request)}`)
}

export function createPurchaseRequest(request: CreatePurchaseRequestRequest): Promise<PurchaseRequestResponse> {
  return apiRequest<PurchaseRequestResponse>('/api/v1/purchase-requests', { method: 'POST', body: request })
}

export function approvePurchaseRequest(id: string, request: ApprovePurchaseRequestRequest): Promise<PurchaseRequestResponse> {
  return apiRequest<PurchaseRequestResponse>(`/api/v1/purchase-requests/${id}/approve`, { method: 'POST', body: request })
}

export function rejectPurchaseRequest(id: string, request: RejectPurchaseRequestRequest): Promise<PurchaseRequestResponse> {
  return apiRequest<PurchaseRequestResponse>(`/api/v1/purchase-requests/${id}/reject`, { method: 'POST', body: request })
}

export function listPurchaseOrders(request: PagedRequest = {}): Promise<PagedResult<PurchaseOrderResponse>> {
  return apiRequest<PagedResult<PurchaseOrderResponse>>(`/api/v1/purchase-orders${pagedQuery(request)}`)
}

export function createPurchaseOrder(request: CreatePurchaseOrderRequest): Promise<PurchaseOrderResponse> {
  return apiRequest<PurchaseOrderResponse>('/api/v1/purchase-orders', { method: 'POST', body: request })
}

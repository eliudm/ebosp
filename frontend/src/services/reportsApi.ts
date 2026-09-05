// Mirrors src/EBOSP.Api/Controllers/ReportsController.cs - read-only, [Authorize] only (see that
// controller's own doc comment for why no extra permission is needed here).

import { apiRequest } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type {
  InventoryReportResponse,
  LowStockReportItem,
  OutstandingInvoiceItem,
  ProcurementSpendReportResponse,
  SalesReportResponse,
  SlowMovingInventoryItem,
  TopProductItem,
} from '../types/reporting'

export interface DateRange {
  from?: string
  to?: string
}

function buildQuery(params: Record<string, string | number | boolean | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      search.set(key, String(value))
    }
  }
  const query = search.toString()
  return query ? `?${query}` : ''
}

function pagedQuery(request: PagedRequest = {}): Record<string, string | number | boolean | undefined> {
  return { page: request.page, pageSize: request.pageSize, sortBy: request.sortBy, sortDescending: request.sortDescending }
}

export function getSalesReport(range: DateRange = {}): Promise<SalesReportResponse> {
  return apiRequest<SalesReportResponse>(`/api/v1/reports/sales${buildQuery({ ...range })}`)
}

export function getInventoryReport(range: DateRange = {}): Promise<InventoryReportResponse> {
  return apiRequest<InventoryReportResponse>(`/api/v1/reports/inventory${buildQuery({ ...range })}`)
}

export function getLowStock(request: PagedRequest = {}): Promise<PagedResult<LowStockReportItem>> {
  return apiRequest<PagedResult<LowStockReportItem>>(`/api/v1/reports/low-stock${buildQuery(pagedQuery(request))}`)
}

export function getSlowMovingInventory(daysInactive: number, request: PagedRequest = {}): Promise<PagedResult<SlowMovingInventoryItem>> {
  return apiRequest<PagedResult<SlowMovingInventoryItem>>(
    `/api/v1/reports/slow-moving-inventory${buildQuery({ daysInactive, ...pagedQuery(request) })}`,
  )
}

export function getProcurementSpendReport(range: DateRange = {}): Promise<ProcurementSpendReportResponse> {
  return apiRequest<ProcurementSpendReportResponse>(`/api/v1/reports/procurement-spend${buildQuery({ ...range })}`)
}

export function getOutstandingInvoices(request: PagedRequest = {}): Promise<PagedResult<OutstandingInvoiceItem>> {
  return apiRequest<PagedResult<OutstandingInvoiceItem>>(`/api/v1/reports/outstanding-invoices${buildQuery(pagedQuery(request))}`)
}

export function getTopProducts(range: DateRange = {}, top = 5): Promise<TopProductItem[]> {
  return apiRequest<TopProductItem[]>(`/api/v1/reports/top-products${buildQuery({ ...range, top })}`)
}

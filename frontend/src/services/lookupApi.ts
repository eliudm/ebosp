// Resolves the raw ids that LowStockReportItem/SlowMovingInventoryItem/OutstandingInvoiceItem
// carry (see docs on ReportsLayout) into display names, by calling the existing plain-[Authorize]
// list endpoints and joining client-side - no backend change. Known simplification: fetches a
// single page at the backend's own max pageSize (200) rather than paginating through everything,
// so a tenant with more entities than that would see blank names for the overflow.

import { apiRequest } from './apiClient'
import type { PagedResult } from '../types/common'

const LOOKUP_PAGE_SIZE = 200

interface NamedEntity {
  id: string
  name: string
}

async function listNames(path: string): Promise<Map<string, string>> {
  const result = await apiRequest<PagedResult<NamedEntity>>(`${path}?pageSize=${LOOKUP_PAGE_SIZE}`)
  return new Map(result.items.map((item) => [item.id, item.name]))
}

export function listProductNames(): Promise<Map<string, string>> {
  return listNames('/api/v1/products')
}

export function listWarehouseNames(): Promise<Map<string, string>> {
  return listNames('/api/v1/warehouses')
}

export function listCustomerNames(): Promise<Map<string, string>> {
  return listNames('/api/v1/customers')
}

export function listBranchNames(): Promise<Map<string, string>> {
  return listNames('/api/v1/branches')
}

export function listSupplierNames(): Promise<Map<string, string>> {
  return listNames('/api/v1/suppliers')
}

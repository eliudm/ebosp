// Mirrors src/EBOSP.Api/Controllers/{Branches,Warehouses,Products}Controller.cs. List/GetById need
// only authentication; create needs master-data.manage (enforced server-side - a user without it
// gets a 403 from these same calls, surfaced via ApiError).

import { apiRequest } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type { BranchResponse, CreateBranchRequest, CreateProductRequest, CreateWarehouseRequest, ProductResponse, WarehouseResponse } from '../types/masterData'

function pagedQuery(request: PagedRequest = {}): string {
  const search = new URLSearchParams()
  if (request.page !== undefined) search.set('page', String(request.page))
  if (request.pageSize !== undefined) search.set('pageSize', String(request.pageSize))
  const query = search.toString()
  return query ? `?${query}` : ''
}

export function listBranches(request: PagedRequest = {}): Promise<PagedResult<BranchResponse>> {
  return apiRequest<PagedResult<BranchResponse>>(`/api/v1/branches${pagedQuery(request)}`)
}

export function createBranch(request: CreateBranchRequest): Promise<BranchResponse> {
  return apiRequest<BranchResponse>('/api/v1/branches', { method: 'POST', body: request })
}

export function listWarehouses(request: PagedRequest = {}): Promise<PagedResult<WarehouseResponse>> {
  return apiRequest<PagedResult<WarehouseResponse>>(`/api/v1/warehouses${pagedQuery(request)}`)
}

export function createWarehouse(request: CreateWarehouseRequest): Promise<WarehouseResponse> {
  return apiRequest<WarehouseResponse>('/api/v1/warehouses', { method: 'POST', body: request })
}

export function listProducts(request: PagedRequest = {}): Promise<PagedResult<ProductResponse>> {
  return apiRequest<PagedResult<ProductResponse>>(`/api/v1/products${pagedQuery(request)}`)
}

export function createProduct(request: CreateProductRequest): Promise<ProductResponse> {
  return apiRequest<ProductResponse>('/api/v1/products', { method: 'POST', body: request })
}

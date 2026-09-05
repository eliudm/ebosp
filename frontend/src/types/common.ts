// Mirrors src/EBOSP.Contracts/Common/PagedRequest.cs and PagedResult.cs.

export interface PagedRequest {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

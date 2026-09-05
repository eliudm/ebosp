// Mirrors src/EBOSP.Contracts/MasterData/*.cs (Product/Warehouse/Branch only - categories are out
// of scope for this pass, see frontend/src/features/inventory/README.md).

export interface CreateBranchRequest {
  name: string
}

export interface BranchResponse {
  id: string
  name: string
  status: string
}

export interface CreateWarehouseRequest {
  branchId: string
  name: string
}

export interface WarehouseResponse {
  id: string
  branchId: string
  name: string
  status: string
}

export interface CreateProductRequest {
  categoryId?: string
  sku: string
  name: string
  description?: string
  unitPrice: number
  taxRatePercent: number
  reorderLevel: number
}

export interface ProductResponse {
  id: string
  categoryId: string | null
  sku: string
  name: string
  description: string | null
  unitPrice: number
  taxRatePercent: number
  reorderLevel: number
  status: string
}

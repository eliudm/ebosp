// Mirrors src/EBOSP.Contracts/Procurement/*.cs.

export interface CreateSupplierRequest {
  name: string
  taxId?: string
  contact?: string
}

export interface SupplierResponse {
  id: string
  name: string
  taxId: string | null
  contact: string | null
  status: string
}

export interface PurchaseRequestLine {
  productId: string
  quantity: number
  estimatedUnitPrice: number
}

export interface CreatePurchaseRequestRequest {
  branchId: string
  justification: string
  requiredDate: string
  lines: PurchaseRequestLine[]
}

export interface ApprovePurchaseRequestRequest {
  notes?: string
}

export interface RejectPurchaseRequestRequest {
  notes: string
}

export interface PurchaseRequestResponse {
  id: string
  requestedByUserId: string
  branchId: string
  justification: string
  estimatedValue: number
  requiredDate: string
  status: 'Pending' | 'Approved' | 'Rejected'
  lines: PurchaseRequestLine[]
}

export interface PurchaseOrderLine {
  productId: string
  quantity: number
  unitPrice: number
}

export interface CreatePurchaseOrderRequest {
  purchaseRequestId: string
  supplierId: string
  poNumber: string
  lines: PurchaseOrderLine[]
}

export interface PurchaseOrderResponse {
  id: string
  purchaseRequestId: string
  supplierId: string
  poNumber: string
  status: 'Open' | 'Cancelled' | 'Fulfilled'
  total: number
  createdByUserId: string
  createdAt: string
  lines: PurchaseOrderLine[]
}

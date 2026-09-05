// Mirrors src/EBOSP.Contracts/Reporting/*.cs.

export interface SalesReportResponse {
  from: string
  to: string
  totalOrders: number
  totalOrderValue: number
  totalInvoices: number
  totalInvoiced: number
  totalCollected: number
  totalOutstanding: number
}

export interface InventoryReportResponse {
  totalValuation: number
  movementFrom: string
  movementTo: string
  totalReceived: number
  totalIssued: number
  totalAdjustedNet: number
  totalTransferred: number
}

export interface LowStockReportItem {
  warehouseId: string
  productId: string
  quantityOnHand: number
  reorderLevel: number
}

export interface SlowMovingInventoryItem {
  warehouseId: string
  productId: string
  quantityOnHand: number
  lastMovementAt: string | null
}

export interface ProcurementSpendBySupplier {
  supplierId: string
  supplierName: string
  total: number
}

export interface ProcurementSpendReportResponse {
  from: string
  to: string
  totalPurchaseOrders: number
  totalSpend: number
  bySupplier: ProcurementSpendBySupplier[]
}

export interface OutstandingInvoiceItem {
  invoiceId: string
  customerId: string
  total: number
  paidTotal: number
  outstanding: number
  createdAt: string
}

export interface TopProductItem {
  productId: string
  productName: string
  quantitySold: number
  revenue: number
}

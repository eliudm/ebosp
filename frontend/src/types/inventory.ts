// Mirrors src/EBOSP.Contracts/Inventory/*.cs.

export interface StockBalanceResponse {
  warehouseId: string
  productId: string
  quantityOnHand: number
  quantityReserved: number
  quantityAvailable: number
}

export interface StockLedgerEntryResponse {
  id: string
  warehouseId: string
  productId: string
  eventType: string
  quantity: number
  actorId: string
  occurredAt: string
  referenceType: string | null
  referenceId: string | null
  reason: string | null
}

export interface ReceiveStockLine {
  productId: string
  quantity: number
}

export interface ReceiveStockRequest {
  warehouseId: string
  purchaseOrderId?: string
  reference?: string
  idempotencyKey?: string
  lines: ReceiveStockLine[]
}

export interface GoodsReceiptResponse {
  id: string
  warehouseId: string
  receivedByUserId: string
  receivedAt: string
  purchaseOrderId: string | null
  reference: string | null
  lines: { productId: string; quantity: number }[]
}

export interface IssueStockRequest {
  warehouseId: string
  productId: string
  quantity: number
  reason?: string
  idempotencyKey?: string
}

export interface TransferStockRequest {
  fromWarehouseId: string
  toWarehouseId: string
  productId: string
  quantity: number
  reason?: string
  idempotencyKey?: string
}

export interface StockTransferResponse {
  id: string
  fromWarehouseId: string
  toWarehouseId: string
  productId: string
  quantity: number
  actorId: string
  occurredAt: string
}

export interface AdjustStockRequest {
  warehouseId: string
  productId: string
  quantityDelta: number
  reason: string
  idempotencyKey?: string
}

export interface StockAdjustmentResponse {
  id: string
  warehouseId: string
  productId: string
  quantityDelta: number
  reason: string
  actorId: string
  occurredAt: string
}

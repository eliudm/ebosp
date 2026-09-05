// Mirrors src/EBOSP.Api/Controllers/InventoryController.cs.

import { apiRequest } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type {
  AdjustStockRequest,
  GoodsReceiptResponse,
  IssueStockRequest,
  ReceiveStockRequest,
  StockAdjustmentResponse,
  StockBalanceResponse,
  StockLedgerEntryResponse,
  StockTransferResponse,
  TransferStockRequest,
} from '../types/inventory'

interface BalanceFilter extends PagedRequest {
  warehouseId?: string
  productId?: string
}

function query(params: Record<string, string | number | boolean | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      search.set(key, String(value))
    }
  }
  const built = search.toString()
  return built ? `?${built}` : ''
}

export function listBalances(filter: BalanceFilter = {}): Promise<PagedResult<StockBalanceResponse>> {
  return apiRequest<PagedResult<StockBalanceResponse>>(`/api/v1/inventory/balances${query({ ...filter })}`)
}

export function listLedger(filter: BalanceFilter = {}): Promise<PagedResult<StockLedgerEntryResponse>> {
  return apiRequest<PagedResult<StockLedgerEntryResponse>>(`/api/v1/inventory/ledger${query({ ...filter })}`)
}

export function receiveStock(request: ReceiveStockRequest): Promise<GoodsReceiptResponse> {
  return apiRequest<GoodsReceiptResponse>('/api/v1/inventory/receipts', { method: 'POST', body: request })
}

export function issueStock(request: IssueStockRequest): Promise<StockLedgerEntryResponse> {
  return apiRequest<StockLedgerEntryResponse>('/api/v1/inventory/issues', { method: 'POST', body: request })
}

export function transferStock(request: TransferStockRequest): Promise<StockTransferResponse> {
  return apiRequest<StockTransferResponse>('/api/v1/inventory/transfers', { method: 'POST', body: request })
}

export function adjustStock(request: AdjustStockRequest): Promise<StockAdjustmentResponse> {
  return apiRequest<StockAdjustmentResponse>('/api/v1/inventory/adjustments', { method: 'POST', body: request })
}

import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { Pagination } from '../../components/Pagination'
import { listLedger } from '../../services/inventoryApi'
import { listProductNames, listWarehouseNames } from '../../services/lookupApi'
import type { PagedResult } from '../../types/common'
import type { StockLedgerEntryResponse } from '../../types/inventory'
import { formatDate } from '../../utils/format'

const PAGE_SIZE = 20

export function LedgerPage() {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<StockLedgerEntryResponse> | null>(null)
  const [productNames, setProductNames] = useState<Map<string, string>>(new Map())
  const [warehouseNames, setWarehouseNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    listProductNames().then(setProductNames)
    listWarehouseNames().then(setWarehouseNames)
  }, [])

  useEffect(() => {
    setIsLoading(true)
    listLedger({ page, pageSize: PAGE_SIZE, sortDescending: true }).then(setResult).finally(() => setIsLoading(false))
  }, [page])

  return (
    <div>
      <h2 className="mb-4 text-lg font-medium text-gray-900">Stock ledger</h2>
      <DataTable
        columns={[
          { header: 'Occurred', render: (row: StockLedgerEntryResponse) => formatDate(row.occurredAt) },
          { header: 'Event', render: (row: StockLedgerEntryResponse) => row.eventType },
          { header: 'Product', render: (row: StockLedgerEntryResponse) => productNames.get(row.productId) ?? row.productId },
          { header: 'Warehouse', render: (row: StockLedgerEntryResponse) => warehouseNames.get(row.warehouseId) ?? row.warehouseId },
          { header: 'Quantity', render: (row: StockLedgerEntryResponse) => row.quantity, align: 'right' },
          { header: 'Reason', render: (row: StockLedgerEntryResponse) => row.reason ?? '—' },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No stock movements yet."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
    </div>
  )
}

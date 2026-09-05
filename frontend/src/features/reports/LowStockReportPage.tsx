import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { Pagination } from '../../components/Pagination'
import { listProductNames, listWarehouseNames } from '../../services/lookupApi'
import { getLowStock } from '../../services/reportsApi'
import type { PagedResult } from '../../types/common'
import type { LowStockReportItem } from '../../types/reporting'

const PAGE_SIZE = 20

export function LowStockReportPage() {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<LowStockReportItem> | null>(null)
  const [productNames, setProductNames] = useState<Map<string, string>>(new Map())
  const [warehouseNames, setWarehouseNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([listProductNames(), listWarehouseNames()])
      .then(([products, warehouses]) => {
        setProductNames(products)
        setWarehouseNames(warehouses)
      })
      .catch(() => {
        // Names are a display nicety, not load-bearing - fall back to showing raw ids.
      })
  }, [])

  useEffect(() => {
    setIsLoading(true)
    setError(null)
    getLowStock({ page, pageSize: PAGE_SIZE })
      .then(setResult)
      .catch(() => setError('Could not load the low-stock report.'))
      .finally(() => setIsLoading(false))
  }, [page])

  return (
    <div>
      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      <DataTable
        columns={[
          { header: 'Product', render: (row) => productNames.get(row.productId) ?? row.productId },
          { header: 'Warehouse', render: (row) => warehouseNames.get(row.warehouseId) ?? row.warehouseId },
          { header: 'On hand', render: (row) => row.quantityOnHand, align: 'right' },
          { header: 'Reorder level', render: (row) => row.reorderLevel, align: 'right' },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => `${row.warehouseId}-${row.productId}`}
        isLoading={isLoading}
        emptyMessage="Nothing is below its reorder level."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
    </div>
  )
}

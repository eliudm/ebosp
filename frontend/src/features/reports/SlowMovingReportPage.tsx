import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { Pagination } from '../../components/Pagination'
import { listProductNames, listWarehouseNames } from '../../services/lookupApi'
import { getSlowMovingInventory } from '../../services/reportsApi'
import type { PagedResult } from '../../types/common'
import type { SlowMovingInventoryItem } from '../../types/reporting'
import { formatDate } from '../../utils/format'

const PAGE_SIZE = 20
const DEFAULT_DAYS_INACTIVE = 90

export function SlowMovingReportPage() {
  const [daysInactive, setDaysInactive] = useState(DEFAULT_DAYS_INACTIVE)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<SlowMovingInventoryItem> | null>(null)
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
    getSlowMovingInventory(daysInactive, { page, pageSize: PAGE_SIZE })
      .then(setResult)
      .catch(() => setError('Could not load the slow-moving inventory report.'))
      .finally(() => setIsLoading(false))
  }, [daysInactive, page])

  return (
    <div>
      <form
        className="mb-4 flex items-end gap-3"
        onSubmit={(event) => {
          event.preventDefault()
          setPage(1)
        }}
      >
        <div>
          <label htmlFor="daysInactive" className="mb-1 block text-xs font-medium text-gray-500">
            Inactive for at least (days)
          </label>
          <input
            id="daysInactive"
            type="number"
            min={1}
            value={daysInactive}
            onChange={(event) => setDaysInactive(Number(event.target.value) || DEFAULT_DAYS_INACTIVE)}
            className="w-32 rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
      </form>
      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      <DataTable
        columns={[
          { header: 'Product', render: (row) => productNames.get(row.productId) ?? row.productId },
          { header: 'Warehouse', render: (row) => warehouseNames.get(row.warehouseId) ?? row.warehouseId },
          { header: 'On hand', render: (row) => row.quantityOnHand, align: 'right' },
          { header: 'Last movement', render: (row) => formatDate(row.lastMovementAt) },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => `${row.warehouseId}-${row.productId}`}
        isLoading={isLoading}
        emptyMessage="Nothing has been inactive that long."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
    </div>
  )
}

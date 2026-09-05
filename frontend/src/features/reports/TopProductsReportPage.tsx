import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { DateRangeFilter } from '../../components/DateRangeFilter'
import { getTopProducts } from '../../services/reportsApi'
import type { TopProductItem } from '../../types/reporting'
import { formatCurrency, formatNumber } from '../../utils/format'

export function TopProductsReportPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [items, setItems] = useState<TopProductItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function load(range: { from?: string; to?: string }) {
    setIsLoading(true)
    setError(null)
    getTopProducts(range, 10)
      .then(setItems)
      .catch(() => setError('Could not load the top products report.'))
      .finally(() => setIsLoading(false))
  }

  useEffect(() => load({}), [])

  return (
    <div>
      <DateRangeFilter
        from={from}
        to={to}
        onFromChange={setFrom}
        onToChange={setTo}
        onApply={() => load({ from: from ? new Date(from).toISOString() : undefined, to: to ? new Date(to).toISOString() : undefined })}
        isLoading={isLoading}
      />
      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      <DataTable
        columns={[
          { header: 'Product', render: (row) => row.productName },
          { header: 'Quantity sold', render: (row) => formatNumber(row.quantitySold), align: 'right' },
          { header: 'Revenue', render: (row) => formatCurrency(row.revenue), align: 'right' },
        ]}
        rows={items}
        keyFor={(row) => row.productId}
        isLoading={isLoading}
        emptyMessage="No sales in this date range."
      />
    </div>
  )
}

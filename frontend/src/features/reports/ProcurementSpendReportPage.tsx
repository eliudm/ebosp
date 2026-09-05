import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { DateRangeFilter } from '../../components/DateRangeFilter'
import { StatCard } from '../../components/StatCard'
import { getProcurementSpendReport } from '../../services/reportsApi'
import type { ProcurementSpendReportResponse } from '../../types/reporting'
import { formatCurrency } from '../../utils/format'

export function ProcurementSpendReportPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [report, setReport] = useState<ProcurementSpendReportResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function load(range: { from?: string; to?: string }) {
    setIsLoading(true)
    setError(null)
    getProcurementSpendReport(range)
      .then(setReport)
      .catch(() => setError('Could not load the procurement spend report.'))
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
      {report && (
        <>
          <div className="mb-6 grid grid-cols-2 gap-4 md:grid-cols-3">
            <StatCard label="Purchase orders" value={String(report.totalPurchaseOrders)} />
            <StatCard label="Total spend" value={formatCurrency(report.totalSpend)} />
          </div>
          <DataTable
            columns={[
              { header: 'Supplier', render: (row) => row.supplierName },
              { header: 'Spend', render: (row) => formatCurrency(row.total), align: 'right' },
            ]}
            rows={report.bySupplier}
            keyFor={(row) => row.supplierId}
            isLoading={isLoading}
            emptyMessage="No purchase orders in this date range."
          />
        </>
      )}
    </div>
  )
}

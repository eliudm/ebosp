import { useEffect, useState } from 'react'
import { DateRangeFilter } from '../../components/DateRangeFilter'
import { StatCard } from '../../components/StatCard'
import { getSalesReport } from '../../services/financeApi'
import type { SalesReportResponse } from '../../types/reporting'
import { formatCurrency } from '../../utils/format'

/** Reuses the Reports module's own sales-report endpoint - it already has TotalInvoiced/TotalCollected/TotalOutstanding for a date range, which is exactly what a first-pass financial summary needs. */
export function SummaryPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [report, setReport] = useState<SalesReportResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function load(range: { from?: string; to?: string }) {
    setIsLoading(true)
    setError(null)
    getSalesReport(range)
      .then(setReport)
      .catch(() => setError('Could not load the financial summary.'))
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
        <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
          <StatCard label="Invoiced" value={formatCurrency(report.totalInvoiced)} />
          <StatCard label="Collected" value={formatCurrency(report.totalCollected)} />
          <StatCard label="Outstanding" value={formatCurrency(report.totalOutstanding)} />
          <StatCard label="Orders" value={String(report.totalOrders)} />
          <StatCard label="Order value" value={formatCurrency(report.totalOrderValue)} />
          <StatCard label="Invoices" value={String(report.totalInvoices)} />
        </div>
      )}
    </div>
  )
}

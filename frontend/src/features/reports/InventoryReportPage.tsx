import { useEffect, useState } from 'react'
import { DateRangeFilter } from '../../components/DateRangeFilter'
import { StatCard } from '../../components/StatCard'
import { getInventoryReport } from '../../services/reportsApi'
import type { InventoryReportResponse } from '../../types/reporting'
import { formatCurrency, formatNumber } from '../../utils/format'

export function InventoryReportPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [report, setReport] = useState<InventoryReportResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function load(range: { from?: string; to?: string }) {
    setIsLoading(true)
    setError(null)
    getInventoryReport(range)
      .then(setReport)
      .catch(() => setError('Could not load the inventory report.'))
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
          <StatCard label="Current valuation" value={formatCurrency(report.totalValuation)} />
          <StatCard label="Received" value={formatNumber(report.totalReceived)} />
          <StatCard label="Issued" value={formatNumber(report.totalIssued)} />
          <StatCard label="Net adjusted" value={formatNumber(report.totalAdjustedNet)} />
          <StatCard label="Transferred in" value={formatNumber(report.totalTransferred)} />
        </div>
      )}
    </div>
  )
}

import { useEffect, useState } from 'react'
import { Badge } from '../../components/Badge'
import { DataTable } from '../../components/DataTable'
import { DateRangeFilter } from '../../components/DateRangeFilter'
import { Pagination } from '../../components/Pagination'
import { getErrorMessage } from '../../services/apiClient'
import { listAuditEvents } from '../../services/securityApi'
import type { PagedResult } from '../../types/common'
import type { AuditEventResponse } from '../../types/security'
import { formatDate } from '../../utils/format'

const PAGE_SIZE = 20

const severityColor: Record<AuditEventResponse['severity'], 'gray' | 'blue' | 'orange' | 'red'> = {
  Low: 'gray',
  Medium: 'blue',
  High: 'orange',
  Critical: 'red',
}

export function AuditEventsPage() {
  const [eventType, setEventType] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<AuditEventResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function load() {
    setIsLoading(true)
    setError(null)
    listAuditEvents({
      eventType: eventType || undefined,
      from: from ? new Date(from).toISOString() : undefined,
      to: to ? new Date(to).toISOString() : undefined,
      page,
      pageSize: PAGE_SIZE,
      sortDescending: true,
    })
      .then(setResult)
      .catch((err) => setError(getErrorMessage(err, 'Could not load audit events.')))
      .finally(() => setIsLoading(false))
  }

  useEffect(load, [page])

  return (
    <div>
      <h2 className="mb-4 text-lg font-medium text-gray-900">Audit events</h2>
      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Event type</label>
          <input
            value={eventType}
            onChange={(event) => setEventType(event.target.value)}
            placeholder="e.g. BranchCreated"
            className="rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        <DateRangeFilter
          from={from}
          to={to}
          onFromChange={setFrom}
          onToChange={setTo}
          onApply={() => {
            setPage(1)
            load()
          }}
          isLoading={isLoading}
        />
      </div>
      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      <DataTable
        columns={[
          { header: 'Occurred', render: (row: AuditEventResponse) => formatDate(row.occurredAt) },
          { header: 'Event type', render: (row: AuditEventResponse) => row.eventType },
          { header: 'Severity', render: (row: AuditEventResponse) => <Badge label={row.severity} color={severityColor[row.severity]} /> },
          { header: 'Aggregate', render: (row: AuditEventResponse) => `${row.aggregateType} (${row.aggregateId.slice(0, 8)})` },
          { header: 'Payload', render: (row: AuditEventResponse) => <span className="line-clamp-1 max-w-xs text-xs text-gray-500">{row.payload}</span> },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No audit events match this filter."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
    </div>
  )
}

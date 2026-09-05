import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Badge } from '../../components/Badge'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { getErrorMessage } from '../../services/apiClient'
import { acknowledgeAlert, investigateAlert, listSecurityAlerts, markFalsePositiveAlert, resolveAlert } from '../../services/securityApi'
import type { PagedResult } from '../../types/common'
import type { SecurityAlertResponse } from '../../types/security'
import { formatDate } from '../../utils/format'

const PAGE_SIZE = 20

const severityColor: Record<SecurityAlertResponse['severity'], 'gray' | 'blue' | 'orange' | 'red'> = {
  Low: 'gray',
  Medium: 'blue',
  High: 'orange',
  Critical: 'red',
}

const statusColor: Record<SecurityAlertResponse['status'], 'gray' | 'blue' | 'yellow' | 'green'> = {
  Open: 'gray',
  Acknowledged: 'blue',
  Investigating: 'yellow',
  Resolved: 'green',
  FalsePositive: 'green',
}

type NotesModalState = { kind: 'resolve' | 'false-positive'; alert: SecurityAlertResponse } | null

export function SecurityAlertsPage() {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<SecurityAlertResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [notesModal, setNotesModal] = useState<NotesModalState>(null)

  function reload() {
    setIsLoading(true)
    setError(null)
    listSecurityAlerts({ page, pageSize: PAGE_SIZE, sortDescending: true })
      .then(setResult)
      .catch((err) => setError(getErrorMessage(err, 'Could not load security alerts.')))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [page])

  async function handleAcknowledge(alert: SecurityAlertResponse) {
    setActionError(null)
    try {
      await acknowledgeAlert(alert.id)
      reload()
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not acknowledge this alert.'))
    }
  }

  async function handleInvestigate(alert: SecurityAlertResponse) {
    setActionError(null)
    try {
      await investigateAlert(alert.id)
      reload()
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not move this alert to investigating.'))
    }
  }

  return (
    <div>
      <h2 className="mb-4 text-lg font-medium text-gray-900">Security alerts</h2>
      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      {actionError && <p className="mb-4 text-sm text-red-600">{actionError}</p>}
      <DataTable
        columns={[
          { header: 'Rule', render: (row: SecurityAlertResponse) => row.rule },
          { header: 'Severity', render: (row: SecurityAlertResponse) => <Badge label={row.severity} color={severityColor[row.severity]} /> },
          { header: 'Description', render: (row: SecurityAlertResponse) => row.description },
          { header: 'Status', render: (row: SecurityAlertResponse) => <Badge label={row.status} color={statusColor[row.status]} /> },
          { header: 'Raised', render: (row: SecurityAlertResponse) => formatDate(row.createdAt) },
          {
            header: 'Actions',
            render: (row: SecurityAlertResponse) => (
              <div className="flex flex-wrap justify-end gap-2">
                <button type="button" onClick={() => handleAcknowledge(row)} className="text-sm text-violet-600 hover:underline">
                  Acknowledge
                </button>
                <button type="button" onClick={() => handleInvestigate(row)} className="text-sm text-violet-600 hover:underline">
                  Investigate
                </button>
                <button type="button" onClick={() => setNotesModal({ kind: 'resolve', alert: row })} className="text-sm text-green-600 hover:underline">
                  Resolve
                </button>
                <button type="button" onClick={() => setNotesModal({ kind: 'false-positive', alert: row })} className="text-sm text-gray-500 hover:underline">
                  False Positive
                </button>
              </div>
            ),
            align: 'right',
          },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No security alerts."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
      {notesModal && (
        <ResolveAlertModal
          kind={notesModal.kind}
          alert={notesModal.alert}
          onClose={() => setNotesModal(null)}
          onDone={() => {
            setNotesModal(null)
            reload()
          }}
        />
      )}
    </div>
  )
}

function ResolveAlertModal({
  kind,
  alert,
  onClose,
  onDone,
}: {
  kind: 'resolve' | 'false-positive'
  alert: SecurityAlertResponse
  onClose: () => void
  onDone: () => void
}) {
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      if (kind === 'resolve') {
        await resolveAlert(alert.id, { notes })
      } else {
        await markFalsePositiveAlert(alert.id, { notes })
      }
      onDone()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not complete this action.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title={kind === 'resolve' ? 'Resolve alert' : 'Mark as false positive'} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Notes (required)</label>
          <textarea
            required
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
            rows={3}
          />
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Submitting…' : kind === 'resolve' ? 'Resolve' : 'Mark as false positive'}
        </button>
      </form>
    </Modal>
  )
}

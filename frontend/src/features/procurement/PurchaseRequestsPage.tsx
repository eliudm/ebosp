import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { ApiError, getErrorMessage } from '../../services/apiClient'
import { listBranchNames, listProductNames } from '../../services/lookupApi'
import { approvePurchaseRequest, createPurchaseRequest, listPurchaseRequests, rejectPurchaseRequest } from '../../services/procurementApi'
import type { PurchaseRequestResponse } from '../../types/procurement'
import { formatCurrency, formatDate } from '../../utils/format'
import { createEmptyLine, LineItemsEditor } from './LineItemsEditor'
import type { LineItem } from './LineItemsEditor'

type ModalState = { kind: 'create' } | { kind: 'reject'; request: PurchaseRequestResponse } | null

export function PurchaseRequestsPage() {
  const [requests, setRequests] = useState<PurchaseRequestResponse[]>([])
  const [branchNames, setBranchNames] = useState<Map<string, string>>(new Map())
  const [productNames, setProductNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [modal, setModal] = useState<ModalState>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  function reload() {
    setIsLoading(true)
    listPurchaseRequests({ pageSize: 100 })
      .then((result) => setRequests(result.items))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [])
  useEffect(() => {
    listBranchNames().then(setBranchNames)
    listProductNames().then(setProductNames)
  }, [])

  async function handleApprove(request: PurchaseRequestResponse) {
    setActionError(null)
    try {
      await approvePurchaseRequest(request.id, {})
      reload()
    } catch (err) {
      setActionError(describeApprovalError(err))
    }
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Purchase requests</h2>
        <button
          type="button"
          onClick={() => setModal({ kind: 'create' })}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New request
        </button>
      </div>
      {actionError && <p className="mb-4 text-sm text-red-600">{actionError}</p>}
      <DataTable
        columns={[
          { header: 'Branch', render: (row: PurchaseRequestResponse) => branchNames.get(row.branchId) ?? row.branchId },
          { header: 'Justification', render: (row: PurchaseRequestResponse) => row.justification },
          { header: 'Estimated value', render: (row: PurchaseRequestResponse) => formatCurrency(row.estimatedValue), align: 'right' },
          { header: 'Required by', render: (row: PurchaseRequestResponse) => formatDate(row.requiredDate) },
          { header: 'Status', render: (row: PurchaseRequestResponse) => row.status },
          {
            header: 'Actions',
            render: (row: PurchaseRequestResponse) =>
              row.status === 'Pending' ? (
                <div className="flex justify-end gap-2">
                  <button type="button" onClick={() => handleApprove(row)} className="text-sm text-violet-600 hover:underline">
                    Approve
                  </button>
                  <button type="button" onClick={() => setModal({ kind: 'reject', request: row })} className="text-sm text-red-600 hover:underline">
                    Reject
                  </button>
                </div>
              ) : (
                '—'
              ),
            align: 'right',
          },
        ]}
        rows={requests}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No purchase requests yet."
      />
      {modal?.kind === 'create' && (
        <CreatePurchaseRequestModal
          branchNames={branchNames}
          productNames={productNames}
          onClose={() => setModal(null)}
          onCreated={() => {
            setModal(null)
            reload()
          }}
        />
      )}
      {modal?.kind === 'reject' && (
        <RejectRequestModal
          request={modal.request}
          onClose={() => setModal(null)}
          onRejected={() => {
            setModal(null)
            reload()
          }}
        />
      )}
    </div>
  )
}

function describeApprovalError(err: unknown): string {
  if (err instanceof ApiError) {
    // A real typed exception (self-approval, already-decided) comes back with a message in
    // problem.title - GlobalExceptionHandler never populates .detail. A bare 403 with no title at
    // all means the static permission check itself failed before the action ran (either the base
    // procurement.approve policy, or the imperative procurement.approve.large check for a
    // high-value request) - can't tell which from the client, so this covers both honestly.
    if (err.problem?.title) {
      return err.problem.title
    }
    if (err.status === 403) {
      return 'You do not have permission to approve or reject this request (it may also be large enough to need the additional large-value approval permission).'
    }
  }
  return 'Could not complete this action.'
}

function CreatePurchaseRequestModal({
  branchNames,
  productNames,
  onClose,
  onCreated,
}: {
  branchNames: Map<string, string>
  productNames: Map<string, string>
  onClose: () => void
  onCreated: () => void
}) {
  const [branchId, setBranchId] = useState('')
  const [justification, setJustification] = useState('')
  const [requiredDate, setRequiredDate] = useState('')
  const [lines, setLines] = useState<LineItem[]>([createEmptyLine()])
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const branchOptions = Array.from(branchNames.entries())

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createPurchaseRequest({
        branchId,
        justification,
        requiredDate: new Date(requiredDate).toISOString(),
        lines: lines.map((line) => ({ productId: line.productId, quantity: line.quantity, estimatedUnitPrice: line.unitPrice })),
      })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the purchase request.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New purchase request" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Branch</label>
          <select required value={branchId} onChange={(event) => setBranchId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm">
            <option value="" disabled>
              {branchOptions.length === 0 ? 'No branches yet' : 'Select a branch'}
            </option>
            {branchOptions.map(([id, name]) => (
              <option key={id} value={id}>
                {name}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Justification</label>
          <input
            required
            value={justification}
            onChange={(event) => setJustification(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Required by</label>
          <input
            type="date"
            required
            value={requiredDate}
            onChange={(event) => setRequiredDate(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        <LineItemsEditor productNames={productNames} lines={lines} onChange={setLines} priceLabel="Est. price" />
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Submitting…' : 'Submit request'}
        </button>
      </form>
    </Modal>
  )
}

function RejectRequestModal({ request, onClose, onRejected }: { request: PurchaseRequestResponse; onClose: () => void; onRejected: () => void }) {
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await rejectPurchaseRequest(request.id, { notes })
      onRejected()
    } catch (err) {
      setError(describeApprovalError(err))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="Reject purchase request" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Reason (required)</label>
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
          className="w-full rounded-md bg-red-600 px-3 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Rejecting…' : 'Reject'}
        </button>
      </form>
    </Modal>
  )
}

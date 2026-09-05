import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { createEmptyLine, LineItemsEditor } from '../../components/LineItemsEditor'
import type { LineItem } from '../../components/LineItemsEditor'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { listBranchNames, listCustomerNames, listProductNames } from '../../services/lookupApi'
import { acceptQuotation, createQuotation, listQuotations, rejectQuotation } from '../../services/salesApi'
import type { QuotationResponse } from '../../types/sales'
import { formatCurrency } from '../../utils/format'

export function QuotationsPage() {
  const [quotations, setQuotations] = useState<QuotationResponse[]>([])
  const [customerNames, setCustomerNames] = useState<Map<string, string>>(new Map())
  const [branchNames, setBranchNames] = useState<Map<string, string>>(new Map())
  const [productNames, setProductNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  function reload() {
    setIsLoading(true)
    listQuotations({ pageSize: 100 })
      .then((result) => setQuotations(result.items))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [])
  useEffect(() => {
    listCustomerNames().then(setCustomerNames)
    listBranchNames().then(setBranchNames)
    listProductNames().then(setProductNames)
  }, [])

  async function handleAccept(quotation: QuotationResponse) {
    setActionError(null)
    try {
      await acceptQuotation(quotation.id)
      reload()
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not accept this quotation.'))
    }
  }

  async function handleReject(quotation: QuotationResponse) {
    setActionError(null)
    try {
      await rejectQuotation(quotation.id)
      reload()
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not reject this quotation.'))
    }
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Quotations</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New quotation
        </button>
      </div>
      {actionError && <p className="mb-4 text-sm text-red-600">{actionError}</p>}
      <DataTable
        columns={[
          { header: 'Customer', render: (row: QuotationResponse) => customerNames.get(row.customerId) ?? row.customerId },
          { header: 'Branch', render: (row: QuotationResponse) => branchNames.get(row.branchId) ?? row.branchId },
          { header: 'Total', render: (row: QuotationResponse) => formatCurrency(row.total), align: 'right' },
          { header: 'Status', render: (row: QuotationResponse) => row.status },
          {
            header: 'Actions',
            render: (row: QuotationResponse) =>
              row.status === 'Pending' ? (
                <div className="flex justify-end gap-2">
                  <button type="button" onClick={() => handleAccept(row)} className="text-sm text-violet-600 hover:underline">
                    Accept
                  </button>
                  <button type="button" onClick={() => handleReject(row)} className="text-sm text-red-600 hover:underline">
                    Reject
                  </button>
                </div>
              ) : (
                '—'
              ),
            align: 'right',
          },
        ]}
        rows={quotations}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No quotations yet."
      />
      {isModalOpen && (
        <CreateQuotationModal
          customerNames={customerNames}
          branchNames={branchNames}
          productNames={productNames}
          onClose={() => setIsModalOpen(false)}
          onCreated={() => {
            setIsModalOpen(false)
            reload()
          }}
        />
      )}
    </div>
  )
}

function CreateQuotationModal({
  customerNames,
  branchNames,
  productNames,
  onClose,
  onCreated,
}: {
  customerNames: Map<string, string>
  branchNames: Map<string, string>
  productNames: Map<string, string>
  onClose: () => void
  onCreated: () => void
}) {
  const [customerId, setCustomerId] = useState('')
  const [branchId, setBranchId] = useState('')
  const [lines, setLines] = useState<LineItem[]>([createEmptyLine()])
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const customerOptions = Array.from(customerNames.entries())
  const branchOptions = Array.from(branchNames.entries())

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createQuotation({ customerId, branchId, lines })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the quotation.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New quotation" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Customer</label>
          <select required value={customerId} onChange={(event) => setCustomerId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm">
            <option value="" disabled>
              {customerOptions.length === 0 ? 'No customers yet' : 'Select a customer'}
            </option>
            {customerOptions.map(([id, name]) => (
              <option key={id} value={id}>
                {name}
              </option>
            ))}
          </select>
        </div>
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
        <LineItemsEditor productNames={productNames} lines={lines} onChange={setLines} />
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Creating…' : 'Create quotation'}
        </button>
      </form>
    </Modal>
  )
}
